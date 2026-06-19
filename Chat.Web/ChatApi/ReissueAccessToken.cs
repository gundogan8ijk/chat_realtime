using Chat.Core.AccountAgg;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class ReissueAccessToken(
    ITokenService tokenService,
    ICookieService cookieService)
  : Endpoint<ReissueAccessTokenRequest, Results<Ok<ReissueAccessTokenResponse>, BadRequest<string>>>
{
  public override void Configure()
  {
    Post("/api/Authen/ReissueAccessToken");
    AllowAnonymous();
  }

  public override async Task<Results<Ok<ReissueAccessTokenResponse>, BadRequest<string>>> ExecuteAsync(ReissueAccessTokenRequest req, CancellationToken ct)
  {
    var refreshToken = HttpContext.Request.Cookies["RefreshToken"];
    if (string.IsNullOrEmpty(refreshToken))
    {
      return TypedResults.BadRequest("Đăng nhập để có refresh token");
    }

    if (string.IsNullOrEmpty(req.DeviceId))
    {
      return TypedResults.BadRequest("Cần thêm thông tin thiết bị từ FE");
    }

    var result = await tokenService.RenewAccessTokenAsync(refreshToken, DeviceId.From(req.DeviceId));
    if (!result.IsSuccess)
    {
      var errorMsg = result.ValidationErrors.FirstOrDefault()?.ErrorMessage ?? "Tái cấp access token thất bại";
      return TypedResults.BadRequest(errorMsg);
    }

    cookieService.SetTokenCookies(result.Value);

    var ttlAccessToken = new DateTimeOffset(result.Value.ExpiresAt).ToUnixTimeMilliseconds().ToString();
    return TypedResults.Ok(new ReissueAccessTokenResponse
    {
      AccessTokenExp = ttlAccessToken
    });
  }
}

public class ReissueAccessTokenRequest
{
  public string DeviceId { get; set; } = string.Empty;
}

public class ReissueAccessTokenResponse
{
  public string AccessTokenExp { get; set; } = string.Empty;
}

