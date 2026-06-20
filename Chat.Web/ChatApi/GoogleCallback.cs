using System.Security.Claims;
using System.Text;
using Chat.Core.AccountAgg;
using Chat.UseCases.ChatApp.Commands;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class GoogleCallback(
    ICookieService cookieService,
    IMediator mediator)
  : EndpointWithoutRequest<Results<RedirectHttpResult, BadRequest<string>>>
{
  private readonly ICookieService _cookieService = cookieService;
  private readonly IMediator _mediator = mediator;

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

    var command = new GoogleCallbackCommand(email, googleId, firstName, lastName, avatarUrlStr, deviceIdStr);
    var result = await _mediator.Send(command, ct);

    if (!result.IsSuccess)
    {
      return TypedResults.BadRequest(result.Errors.FirstOrDefault() ?? "Google authentication failed");
    }

    var val = result.Value;
    _cookieService.SetTokenCookies(val.AccessToken, val.RefreshToken);

    var accessExp = new DateTimeOffset(val.AccessToken.ExpiresAt).ToUnixTimeMilliseconds().ToString();
    var refreshExp = new DateTimeOffset(val.RefreshToken.ExpiresAt).ToUnixTimeMilliseconds().ToString();
    var queryParams = $"accessTokenExp={accessExp}&refreshTokenExp={refreshExp}";
    var base64Query = Convert.ToBase64String(Encoding.UTF8.GetBytes(queryParams));

    return TypedResults.Redirect($"http://localhost:5173/?tokenExpiresAt={base64Query}");
  }
}

