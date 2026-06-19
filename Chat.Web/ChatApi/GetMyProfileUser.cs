using System.Security.Claims;
using Chat.UseCases.ChatApp;
using Chat.UseCases.ChatApp.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class GetMyProfileUser(IMediator mediator)
  : EndpointWithoutRequest<Results<Ok<UserDto>, BadRequest<string>, UnauthorizedHttpResult>>
{
  public override void Configure()
  {
    Get("/api/GetMyProfileUser/Profile");
  }

  public override async Task<Results<Ok<UserDto>, BadRequest<string>, UnauthorizedHttpResult>> ExecuteAsync(CancellationToken ct)
  {
    var userIdString = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var myId))
    {
      return TypedResults.Unauthorized();
    }

    var query = new GetMyProfileQuery(myId);
    var result = await mediator.Send(query, ct);

    if (!result.IsSuccess)
    {
      var errorMsg = result.Errors.FirstOrDefault() ?? "Không thể lấy thông tin profile";
      return TypedResults.BadRequest(errorMsg);
    }

    return TypedResults.Ok(result.Value);
  }
}

