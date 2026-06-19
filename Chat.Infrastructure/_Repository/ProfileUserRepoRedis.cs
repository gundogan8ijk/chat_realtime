using Chat.Core.AccountAgg;

using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace Chat.Infrastructure._Repository;

public class ProfileUserRepoRedis : IProfileUserRepoRedis
{
  private readonly IDatabase _dbRedis;
  private readonly ILogger<ProfileUserRepoRedis> _logger;

  public ProfileUserRepoRedis(
      IConnectionMultiplexer redis,
      IConfiguration configuration,
      ILogger<ProfileUserRepoRedis> logger)
  {
    var stackUserIndex = int.Parse(configuration["Redis:stackUser"] ?? "1");
    _dbRedis = redis.GetDatabase(stackUserIndex);
    _logger = logger;
  }

  public async Task<bool> AddProfileUserRedisAsync(UserProfile profile, string email, CancellationToken ct = default)
  {
    try
    {
      string key = profile.Id.Value.ToString();

      var userData = new HashEntry[]
      {
        new HashEntry("email", email),
        new HashEntry("lastName", profile.LastName.Value),
        new HashEntry("firstName", profile.FirstName.Value),
        new HashEntry("avatarURL", profile.AvatarUrl?.Value ?? string.Empty),
        new HashEntry("createDate", ((DateTimeOffset)profile.CreateDate).ToUnixTimeMilliseconds().ToString())
      };

      await _dbRedis.HashSetAsync(key, userData);
      await _dbRedis.StringSetAsync(email, key);

      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Lỗi khi lưu profile user {Email} vào Redis", email);
      return false;
    }
  }
}

