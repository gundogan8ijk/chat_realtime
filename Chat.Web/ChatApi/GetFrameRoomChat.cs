using System.Security.Claims;
using Chat.UseCases.ChatApp;
using Chat.UseCases.ChatApp.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class GetFrameRoomChat(IMediator mediator)
  : Endpoint<GetFrameRoomChatRequest, Results<Ok<FrameDialoChatDto>, BadRequest<string>, UnauthorizedHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/api/roomchat/{RoomId}");
  }

  public override async Task<Results<Ok<FrameDialoChatDto>, BadRequest<string>, UnauthorizedHttpResult>>
    ExecuteAsync(GetFrameRoomChatRequest req, CancellationToken ct)
  {
    var userIdString = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var myId))
    {
      // Fallback for testing: if not authenticated, we can mock or return unauthorized
      return TypedResults.Unauthorized();
    }

    var query = new GetFrameRoomChatQuery(myId, req.RoomId, req.PageSize ?? 50, req.PageNumber ?? 1);
    var result = await _mediator.Send(query, ct);

    if (!result.IsSuccess)
    {
      return TypedResults.BadRequest(result.Errors.FirstOrDefault() ?? "Đã xảy ra lỗi");
    }

    return TypedResults.Ok(result.Value);
  }
}

public class GetFrameRoomChatRequest
{
  public Guid RoomId { get; set; }
  public int? PageSize { get; set; }
  public int? PageNumber { get; set; }
}

