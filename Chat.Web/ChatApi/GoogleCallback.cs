using System.Security.Claims;
using System.Text;
using Chat.Core.AccountAgg;

using Chat.Infrastructure.Data.Context;
using Chat.UseCases.ChatApp.Commands;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chat.Web.ChatApi;

public class GoogleCallback(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ChatDbContext dbContext,
    ITokenService tokenService,
    ICookieService cookieService,
    IMediator mediator)
  : EndpointWithoutRequest<Results<RedirectHttpResult, BadRequest<string>>>
{
  public override void Configure()
  {
    Get("/api/auth/google/callback");
    AllowAnonymous();
  }

  public override async Task<Results<RedirectHttpResult, BadRequest<string>>> ExecuteAsync(CancellationToken ct)
  {
    var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

    if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
    {
      return TypedResults.BadRequest("Google authentication failed");
    }

    var stateEncoded = authenticateResult.Properties?.Items["state"];
    string? deviceIdStr = null;
    try
    {
      if (!string.IsNullOrEmpty(stateEncoded))
      {
        deviceIdStr = Encoding.UTF8.GetString(Convert.FromBase64String(stateEncoded));
      }
    }
    catch
    {
      return TypedResults.BadRequest("Device ID không hợp lệ");
    }

    if (string.IsNullOrEmpty(deviceIdStr))
    {
      return TypedResults.BadRequest("Missing device ID");
    }

    var claims = authenticateResult.Principal.Claims.ToList();
    var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? string.Empty;
    var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? string.Empty;
    var avatarUrlStr = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value;

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
    {
      return TypedResults.BadRequest("Google account claims missing email or identifier");
    }

    var info = new UserLoginInfo("Google", googleId, "Google");
    var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

    if (user == null)
    {
      user = await userManager.FindByEmailAsync(email);

      if (user == null)
      {
        user = new ApplicationUser
        {
          UserName = email,
          Email = email,
          EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
          var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));
          return TypedResults.BadRequest($"Không thể tạo tài khoản từ Google: {errors}");
        }

        const string defaultRole = "User";
        if (!await roleManager.RoleExistsAsync(defaultRole))
        {
          await roleManager.CreateAsync(new ApplicationRole { Name = defaultRole, ValueRole = 1 });
        }
        await userManager.AddToRoleAsync(user, defaultRole);

        // Save profile to database
        var userId = UserId.From(user.Id);
        var firstNameVal = FirstName.From(firstName);
        var lastNameVal = LastName.From(lastName);
        var avatarUrlVal = string.IsNullOrEmpty(avatarUrlStr) ? (AvatarUrl?)null : AvatarUrl.From(avatarUrlStr);

        var profile = UserProfile.Create(userId, firstNameVal, lastNameVal, avatarUrlVal);
        await dbContext.UserProfiles.AddAsync(profile, ct);
        await dbContext.SaveChangesAsync(ct);

        // Save profile to Redis
        var redisCmd = new AddProfileUserRedisCommand(userId, firstNameVal, lastNameVal, email, avatarUrlVal);
        await mediator.Send(redisCmd, ct);
      }

      var linkResult = await userManager.AddLoginAsync(user, info);
      if (!linkResult.Succeeded)
      {
        var errors = string.Join(", ", linkResult.Errors.Select(x => x.Description));
        return TypedResults.BadRequest($"Không thể liên kết tài khoản Google: {errors}");
      }
    }

    var dbProfile = await dbContext.UserProfiles
        .FirstOrDefaultAsync(p => p.Id == UserId.From(user.Id), ct);

    if (dbProfile == null)
    {
      // If profile is missing (e.g. legacy migrated user), create one
      var userId = UserId.From(user.Id);
      var firstNameVal = FirstName.From(firstName);
      var lastNameVal = LastName.From(lastName);
      var avatarUrlVal = string.IsNullOrEmpty(avatarUrlStr) ? (AvatarUrl?)null : AvatarUrl.From(avatarUrlStr);

      dbProfile = UserProfile.Create(userId, firstNameVal, lastNameVal, avatarUrlVal);
      await dbContext.UserProfiles.AddAsync(dbProfile, ct);
      await dbContext.SaveChangesAsync(ct);

      var redisCmd = new AddProfileUserRedisCommand(userId, firstNameVal, lastNameVal, email, avatarUrlVal);
      await mediator.Send(redisCmd, ct);
    }

    var tokens = await tokenService.GenerateTokensAsync(user, dbProfile, DeviceId.From(deviceIdStr));
    if (tokens.AccessToken == null || tokens.RefreshToken == null)
    {
      return TypedResults.BadRequest("Tạo token thất bại");
    }

    cookieService.SetTokenCookies(tokens.AccessToken, tokens.RefreshToken);

    var accessExp = new DateTimeOffset(tokens.AccessToken.ExpiresAt).ToUnixTimeMilliseconds().ToString();
    var refreshExp = new DateTimeOffset(tokens.RefreshToken.ExpiresAt).ToUnixTimeMilliseconds().ToString();
    var queryParams = $"accessTokenExp={accessExp}&refreshTokenExp={refreshExp}";
    var base64Query = Convert.ToBase64String(Encoding.UTF8.GetBytes(queryParams));

    return TypedResults.Redirect($"http://localhost:5173/?tokenExpiresAt={base64Query}");
  }
}

