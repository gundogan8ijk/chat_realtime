using Chat.Core.AccountAgg;
using Chat.UseCases.ChatApp;
using Microsoft.AspNetCore.Http;

namespace Chat.Infrastructure._Services;

public class CookieService : ICookieService
{
  private readonly IHttpContextAccessor _contextAccessor;

  public CookieService(IHttpContextAccessor contextAccessor)
  {
    _contextAccessor = contextAccessor;
  }

  private const string AccessTokenName = "AccessToken";
  private const string RefreshTokenName = "RefreshToken";

  public void SetTokenCookies(TemplateTokenDto accessToken)
  {
    var options = new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      SameSite = SameSiteMode.None,
      Expires = accessToken.ExpiresAt,
    };

    _contextAccessor.HttpContext?.Response.Cookies.Append(AccessTokenName, accessToken.ContentToken, options);
  }

  public void SetTokenCookies(TemplateTokenDto accessToken, TemplateTokenDto refreshToken)
  {
    var options = new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      SameSite = SameSiteMode.None,
      Expires = accessToken.ExpiresAt,
    };

    _contextAccessor.HttpContext?.Response.Cookies.Append(AccessTokenName, accessToken.ContentToken, options);

    options.Expires = refreshToken.ExpiresAt;
    _contextAccessor.HttpContext?.Response.Cookies.Append(RefreshTokenName, refreshToken.ContentToken, options);
  }

  public void DeleteTokenCookies()
  {
    _contextAccessor.HttpContext?.Response.Cookies.Delete(RefreshTokenName);
    _contextAccessor.HttpContext?.Response.Cookies.Delete(AccessTokenName);
  }
}

