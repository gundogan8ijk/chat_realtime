using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Queries;

public class GetFrameRoomChatQueryHandler(IChatQueryService chatQueryService)
  : IQueryHandler<GetFrameRoomChatQuery, Result<FrameDialoChatDto>>
{
  private readonly IChatQueryService _chatQueryService = chatQueryService;

  public async ValueTask<Result<FrameDialoChatDto>> Handle(
      GetFrameRoomChatQuery request, 
      CancellationToken cancellationToken)
  {
    var res = await _chatQueryService.GetFrameRoomChatAsync(
        request.MyId, 
        request.RoomId, 
        request.PageSize, 
        request.PageNumber, 
        cancellationToken);

    if (res == null)
    {
      return Result<FrameDialoChatDto>.NotFound("Không tìm thấy phòng chat hoặc bạn không thuộc cuộc hội thoại này.");
    }

    return Result.Success(res);
  }
}

