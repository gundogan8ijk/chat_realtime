using System.Security.Claims;
using Chat.UseCases.ChatApp;
using Chat.UseCases.ChatApp.Commands;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class GetOrCreatRoomDialoChat(IMediator mediator)
  : Endpoint<GetOrCreatRoomDialoChatRequest, Results<Ok<string>, BadRequest<string>, UnauthorizedHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post("/api/roomchat/roomChat");
    AllowAnonymous();
  }

  public override async Task<Results<Ok<string>, BadRequest<string>, UnauthorizedHttpResult>>
    ExecuteAsync(GetOrCreatRoomDialoChatRequest req, CancellationToken ct)
  {
    var userIdString = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var myId))
    {
      return TypedResults.Unauthorized();
    }

    if (!Guid.TryParse(req.PartnerId, out var partnerIdGuid))
    {
      return TypedResults.BadRequest("partnerId không hợp lệ");
    }

    var command = new GetOrCreatConversationCommand(myId, partnerIdGuid);
    var result = await _mediator.Send(command, ct);

    if (!result.IsSuccess)
    {
      return TypedResults.BadRequest(result.Errors.FirstOrDefault() ?? "Đã xảy ra lỗi khi tạo phòng chat");
    }

    return TypedResults.Ok(result.Value);
  }
}

public class GetOrCreatRoomDialoChatRequest
{
  [QueryParam]
  public string PartnerId { get; set; } = string.Empty;
}

