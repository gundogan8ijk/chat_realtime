using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class GoogleLogin : EndpointWithoutRequest<Results<ChallengeHttpResult, BadRequest<string>>>
{
  public override void Configure()
  {
    Get("/api/auth/google/login");
    AllowAnonymous();
  }

  public override async Task<Results<ChallengeHttpResult, BadRequest<string>>> ExecuteAsync(CancellationToken ct)
  {
    var deviceIdBase64 = Query<string>("d");
    if (string.IsNullOrEmpty(deviceIdBase64))
    {
      return TypedResults.BadRequest("Missing device ID");
    }

    var properties = new AuthenticationProperties
    {
      RedirectUri = "/api/auth/google/callback",
      Items = { { "state", deviceIdBase64 } }
    };

    return TypedResults.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
  }
}

