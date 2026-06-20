using Chat.Core.AccountAgg;
using Chat.UseCases.ChatApp.Commands;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class SignIn(IMediator mediator, ICookieService cookieService)
  : Endpoint<SignInRequest, Results<Ok<SignInResponse>, BadRequest<string>>>
{
  private readonly IMediator _mediator = mediator;
  private readonly ICookieService _cookieService = cookieService;

  public override void Configure()
  {
    Post("/api/AccountNormal/SignIn");
    AllowAnonymous();
  }

  public override async Task<Results<Ok<SignInResponse>, BadRequest<string>>> ExecuteAsync(SignInRequest req, CancellationToken ct)
  {
    var command = new SignInCommand(req.Email, req.Password, req.DeviceId);
    var result = await _mediator.Send(command, ct);

    if (!result.IsSuccess)
    {
      return TypedResults.BadRequest(result.Errors.FirstOrDefault() ?? "Tài khoản hoặc mật khẩu không chính xác");
    }

    var val = result.Value;
    _cookieService.SetTokenCookies(val.AccessToken, val.RefreshToken);

    var accessTokenExp = new DateTimeOffset(val.AccessToken.ExpiresAt).ToUnixTimeMilliseconds();
    var refreshTokenExp = new DateTimeOffset(val.RefreshToken.ExpiresAt).ToUnixTimeMilliseconds();

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

