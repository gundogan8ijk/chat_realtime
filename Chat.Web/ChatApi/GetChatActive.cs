using System.Security.Claims;
using Chat.UseCases.ChatApp;
using Chat.UseCases.ChatApp.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class GetChatActive(IMediator mediator)
  : Endpoint<GetChatActiveRequest, Results<Ok<List<ItemChatDto>>, BadRequest<string>, UnauthorizedHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/api/chatactive/ListUserChat");
  }

  public override async Task<Results<Ok<List<ItemChatDto>>, BadRequest<string>, UnauthorizedHttpResult>>
    ExecuteAsync(GetChatActiveRequest req, CancellationToken ct)
  {
    var userIdString = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var myId))
    {
      return TypedResults.Unauthorized();
    }

    var query = new GetListItemChatActiveQuery(myId, req.PageSize ?? 10, req.PageNumber ?? 1);
    var result = await _mediator.Send(query, ct);

    if (!result.IsSuccess)
    {
      return TypedResults.BadRequest(result.Errors.FirstOrDefault() ?? "Đã xảy ra lỗi");
    }

    return TypedResults.Ok(result.Value);
  }
}

public class GetChatActiveRequest
{
  [QueryParam]
  public int? PageSize { get; set; }
  
  [QueryParam]
  public int? PageNumber { get; set; }
}

