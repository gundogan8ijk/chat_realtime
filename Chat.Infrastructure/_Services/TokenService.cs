using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Chat.Core.AccountAgg;

using Chat.Infrastructure.Data.Context;
using Chat.UseCases.ChatApp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Ardalis.Result;

namespace Chat.Infrastructure._Services;

public class TokenService : ITokenService
{
  private readonly ChatDbContext _dbContext;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IDatabase _redisDb;
  private readonly IConfiguration _configuration;
  private readonly ILogger<TokenService> _logger;

  private const string AddRefreshTokenScript = @"
local dataInfoUser=cjson.decode(ARGV[1])
local keyToken = KEYS[1]
local keyUser,valueTokenUser = KEYS[2],ARGV[2]
local ttl = tonumber(ARGV[3])
local maxRef = tonumber(ARGV[4])

for field,value in pairs(dataInfoUser) do
    if type(value) == 'table' then
        value = cjson.encode(value)
    end
    redis.call('HSET',keyToken,field,value)
end
redis.call('EXPIRE',keyToken,ttl)
redis.call('RPUSH',keyUser,valueTokenUser)
local count = redis.call('LLEN',keyUser)
if count > maxRef then
    local numToRemove = count-maxRef
    local oldTokens = redis.call('LRANGE',keyUser,0,numToRemove-1)
    for _,oldToken in ipairs(oldTokens) do
        local oldKey = oldToken
        redis.call('DEL',oldKey)
    end
    redis.call('LTRIM',keyUser,-maxRef,-1)
end
return 'OK'
";

  public TokenService(
      ChatDbContext dbContext,
      UserManager<ApplicationUser> userManager,
      IConnectionMultiplexer redis,
      IConfiguration configuration,
      ILogger<TokenService> logger)
  {
    _dbContext = dbContext;
    _userManager = userManager;
    _configuration = configuration;
    _logger = logger;
    var stackTokenIndex = int.Parse(_configuration["Redis:stackToken"] ?? "0");
    _redisDb = redis.GetDatabase(stackTokenIndex);
  }

  public async Task<TokenDto> GenerateTokensAsync(ApplicationUser user, UserProfile profile, DeviceId deviceId)
  {
    var roles = await _userManager.GetRolesAsync(user);
    var claims = CreateClaims(user.Id.ToString(), user.Email ?? string.Empty, roles.ToList());

    var accessToken = GenerateAccessToken(claims);
    var refreshToken = GenerateRefreshToken();

    // 1. Save RefreshToken in DB
    await using var transaction = await _dbContext.Database.BeginTransactionAsync();
    try
    {
      var jwtId = Guid.NewGuid().ToString();
      var rfEntity = RefreshToken.Create(UserId.From(user.Id), refreshToken.ContentToken, jwtId, deviceId, refreshToken.ExpiresAt);

      await _dbContext.RefreshTokens.AddAsync(rfEntity);
      await _dbContext.SaveChangesAsync();

      // Revoke older tokens if they exceed Limit
      var maxDbLimit = int.Parse(_configuration["JWT:MaxRefreshTokensPerUser"] ?? "2");
      await _dbContext.RefreshTokens
          .Where(x => x.UserId == user.Id && !x.IsRevoked)
          .OrderByDescending(x => x.IssuedAt)
          .Skip(maxDbLimit)
          .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRevoked, true));

      await transaction.CommitAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Lỗi khi lưu RefreshToken vào Database");
      await transaction.RollbackAsync();
    }

    // 2. Save RefreshToken in Redis
    try
    {
      var redisUserKey = $"userTokens:{user.Id}";
      var infoUser = new RefreshTokenRedisDto
      {
        DeviceId = deviceId.Value,
        UserId = user.Id.ToString(),
        LastName = profile.LastName.Value,
        FirstName = profile.FirstName.Value,
        Email = user.Email ?? string.Empty,
        Role = roles.ToList()
      };

      var jsonPayload = JsonSerializer.Serialize(infoUser);
      var ttlSeconds = (int)(refreshToken.ExpiresAt - DateTime.UtcNow).TotalSeconds;
      var maxRedisLimit = int.Parse(_configuration["JWT:MaxRefreshTokensPerUser"] ?? "2");

      RedisKey[] keys = { refreshToken.ContentToken, redisUserKey };
      RedisValue[] args = { jsonPayload, refreshToken.ContentToken, ttlSeconds, maxRedisLimit };

      var result = await _redisDb.ScriptEvaluateAsync(AddRefreshTokenScript, keys, args);
      if (result.ToString() != "OK")
      {
        _logger.LogWarning("Không thể ghi RefreshToken vào Redis bằng Lua script");
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Lỗi khi ghi RefreshToken vào Redis");
    }

    return new TokenDto
    {
      AccessToken = accessToken,
      RefreshToken = refreshToken
    };
  }

  public async Task<Result<TemplateTokenDto>> RenewAccessTokenAsync(string refreshToken, DeviceId deviceId)
  {
    // 1. Check Redis first
    try
    {
      var cachedDeviceId = await _redisDb.HashGetAsync(refreshToken, "DeviceId");
      if (cachedDeviceId.HasValue && cachedDeviceId.ToString() == deviceId.Value)
      {
        var values = await _redisDb.HashGetAllAsync(refreshToken);
        var dict = values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        dict.TryGetValue("UserId", out string? userId);
        dict.TryGetValue("Email", out string? email);
        dict.TryGetValue("LastName", out string? lastName);
        dict.TryGetValue("FirstName", out string? firstName);
        dict.TryGetValue("Role", out string? rolesJson);

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email) &&
            !string.IsNullOrEmpty(lastName) && !string.IsNullOrEmpty(firstName))
        {
          var roles = string.IsNullOrEmpty(rolesJson)
              ? new List<string>()
              : JsonSerializer.Deserialize<List<string>>(rolesJson) ?? new List<string>();

          var claims = CreateClaims(userId, email, roles);
          var newAccessToken = GenerateAccessToken(claims);
          return Result.Success(newAccessToken);
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Lỗi khi đọc token từ Redis, sẽ fallback qua Database");
    }

    // 2. Fallback to Database
    var storedToken = await _dbContext.RefreshTokens
        .FirstOrDefaultAsync(x => x.Token == refreshToken && x.DeviceId == deviceId);

    if (storedToken == null)
    {
      return Result.Invalid(new ValidationError("Refresh token không tồn tại"));
    }

    if (storedToken.ExpireAt <= DateTime.UtcNow)
    {
      return Result.Invalid(new ValidationError("Refresh token đã hết hạn"));
    }

    if (storedToken.IsRevoked)
    {
      return Result.Invalid(new ValidationError("Refresh token đã bị thu hồi"));
    }

    if (storedToken.IsUsed)
    {
      return Result.Invalid(new ValidationError("Refresh token đã được sử dụng"));
    }

    var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
    if (user == null)
    {
      return Result.Invalid(new ValidationError("Người dùng không tồn tại"));
    }

    var profile = await _dbContext.UserProfiles
        .FirstOrDefaultAsync(p => p.Id == storedToken.UserId);

    var userRoles = await _userManager.GetRolesAsync(user);

    var dbClaims = CreateClaims(
        user.Id.ToString(),
        user.Email ?? string.Empty,
        userRoles.ToList());

    var tokenDto = GenerateAccessToken(dbClaims);
    return Result.Success(tokenDto);
  }

  private List<Claim> CreateClaims(string userId, string email, List<string> roles)
  {
    var claims = new List<Claim>
    {
      new Claim(JwtRegisteredClaimNames.Sub, userId),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
      new Claim(JwtRegisteredClaimNames.Email, email)
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
    return claims;
  }

  private TemplateTokenDto GenerateAccessToken(List<Claim> claims)
  {
    var jwtTokenHandler = new JwtSecurityTokenHandler();
    var secretKey = _configuration["JWTOptions:Secret"];
    if (string.IsNullOrEmpty(secretKey))
    {
      throw new InvalidOperationException("JWT Secret key is not configured in JWTOptions:Secret.");
    }
    var key = Encoding.UTF8.GetBytes(secretKey);
    var expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JWTOptions:ExpirationTimeInMinutes"] ?? "15"));

    var tokenDescriptor = new JwtSecurityToken(
        issuer: _configuration["JWTOptions:Issuer"] ?? "ChatApi",
        audience: _configuration["JWTOptions:Audience"] ?? "ChatClient",
        expires: expires,
        claims: claims,
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    );

    return new TemplateTokenDto
    {
      ContentToken = jwtTokenHandler.WriteToken(tokenDescriptor),
      ExpiresAt = expires
    };
  }

  private TemplateTokenDto GenerateRefreshToken()
  {
    var random = new byte[32];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(random);

    var validityDays = int.Parse(_configuration["JWT:RefreshTokenValidityInDays"] ?? "7");
    var expires = DateTime.UtcNow.AddDays(validityDays);

    return new TemplateTokenDto
    {
      ContentToken = Convert.ToBase64String(random),
      ExpiresAt = expires
    };
  }
}

public class RefreshTokenRedisDto
{
  public string DeviceId { get; set; } = string.Empty;
  public string UserId { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public List<string> Role { get; set; } = new();
}

