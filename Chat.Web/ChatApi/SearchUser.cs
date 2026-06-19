using System.Security.Claims;
using Chat.UseCases.ChatApp;
using Chat.UseCases.ChatApp.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class SearchUser(IMediator mediator)
  : Endpoint<SearchUserRequest, Results<Ok<UserSearchDto>, BadRequest<string>, UnauthorizedHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/api/searchuser/email");
    AllowAnonymous();
  }

  public override async Task<Results<Ok<UserSearchDto>, BadRequest<string>, UnauthorizedHttpResult>>
    ExecuteAsync(SearchUserRequest req, CancellationToken ct)
  {
    var userIdString = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var myId))
    {
      return TypedResults.Unauthorized();
    }

    var myEmail = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "myemail@example.com";

    if (string.IsNullOrWhiteSpace(req.Email))
    {
      return TypedResults.BadRequest("Email không được để trống");
    }

    var query = new GetUserByEmailQuery(req.Email, myEmail, myId);
    var result = await _mediator.Send(query, ct);

    if (!result.IsSuccess)
    {
      return TypedResults.BadRequest(result.Errors.FirstOrDefault() ?? "Không tìm thấy người dùng");
    }

    return TypedResults.Ok(result.Value);
  }
}

public class SearchUserRequest
{
  [QueryParam]
  public string Email { get; set; } = string.Empty;
}

