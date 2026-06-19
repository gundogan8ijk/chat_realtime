namespace Chat.Core.AccountAgg;

public interface ICookieService
{
  void SetTokenCookies(TemplateTokenDto accessToken);
  void SetTokenCookies(TemplateTokenDto accessToken, TemplateTokenDto refreshToken);
  void DeleteTokenCookies();
}

