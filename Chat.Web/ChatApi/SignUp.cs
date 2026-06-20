using Chat.UseCases.ChatApp.Commands;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class SignUp(IMediator mediator)
  : Endpoint<SignUpRequest, Results<Ok<SignUpResponse>, BadRequest<string>>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post("/api/AccountNormal/SignUp");
    AllowAnonymous();
  }

  public override async Task<Results<Ok<SignUpResponse>, BadRequest<string>>> ExecuteAsync(SignUpRequest req, CancellationToken ct)
  {
    var command = new SignUpCommand(req.Email, req.Password, req.FirstName, req.LastName);
    var result = await _mediator.Send(command, ct);

    if (!result.IsSuccess)
    {
      return TypedResults.BadRequest(result.Errors.FirstOrDefault() ?? "Đăng ký thất bại");
    }

    var val = result.Value;
    return TypedResults.Ok(new SignUpResponse
    {
      Succeeded = val.Succeeded,
      Email = val.Email,
      LastName = val.LastName,
      FirstName = val.FirstName
    });
  }
}

public class SignUpRequest
{
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
}

public class SignUpResponse
{
  public bool Succeeded { get; set; }
  public string Email { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
}

