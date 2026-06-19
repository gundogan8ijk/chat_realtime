using Chat.Core.AccountAgg;

using Chat.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chat.Web.ChatApi;

public class SignIn(
    UserManager<ApplicationUser> userManager,
    ChatDbContext dbContext,
    ITokenService tokenService,
    ICookieService cookieService)
  : Endpoint<SignInRequest, Results<Ok<SignInResponse>, BadRequest<string>>>
{
  public override void Configure()
  {
    Post("/api/AccountNormal/SignIn");
    AllowAnonymous();
  }

  public override async Task<Results<Ok<SignInResponse>, BadRequest<string>>> ExecuteAsync(SignInRequest req, CancellationToken ct)
  {
    var user = await userManager.FindByEmailAsync(req.Email);
    if (user == null || !await userManager.CheckPasswordAsync(user, req.Password))
    {
      return TypedResults.BadRequest("Tài khoản hoặc mật khẩu không chính xác");
    }

    var profile = await dbContext.UserProfiles
        .FirstOrDefaultAsync(p => p.Id == UserId.From(user.Id), ct);

    if (profile == null)
    {
      return TypedResults.BadRequest("Không tìm thấy thông tin profile của người dùng");
    }

    var tokens = await tokenService.GenerateTokensAsync(user, profile, DeviceId.From(req.DeviceId));
    if (tokens.AccessToken == null || tokens.RefreshToken == null)
    {
      return TypedResults.BadRequest("Tạo token thất bại");
    }

    cookieService.SetTokenCookies(tokens.AccessToken, tokens.RefreshToken);

    var accessTokenExp = new DateTimeOffset(tokens.AccessToken.ExpiresAt).ToUnixTimeMilliseconds();
    var refreshTokenExp = new DateTimeOffset(tokens.RefreshToken.ExpiresAt).ToUnixTimeMilliseconds();

    return TypedResults.Ok(new SignInResponse
    {
      AccessTokenExp = accessTokenExp,
      RefreshTokenExp = refreshTokenExp
    });
  }
}

public class SignInRequest
{
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string DeviceId { get; set; } = string.Empty;
}

public class SignInResponse
{
  public long AccessTokenExp { get; set; }
  public long RefreshTokenExp { get; set; }
}

