using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Commands;

public record GetOrCreatConversationCommand(Guid MyId, Guid PartnerId) : ICommand<Result<string>>;

public class GetOrCreatConversationCommandHandler(IChatQueryService chatQueryService, IChatCommandService chatCommandService)
  : ICommandHandler<GetOrCreatConversationCommand, Result<string>>
{
  private readonly IChatQueryService _chatQueryService = chatQueryService;
  private readonly IChatCommandService _chatCommandService = chatCommandService;

  public async ValueTask<Result<string>> Handle(GetOrCreatConversationCommand request, CancellationToken cancellationToken)
  {
    string roomId = await _chatQueryService.GetIdConversationChatAsync(request.MyId, request.PartnerId, cancellationToken) ?? string.Empty;

    if (string.IsNullOrEmpty(roomId))
    {
      var res = await _chatCommandService.CreateRoomMessageAsync(request.MyId, request.PartnerId, cancellationToken);
      if (!res.IsSuccess)
      {
        return Result<string>.Error(res.Errors.FirstOrDefault() ?? "Không thể tạo phòng chat");
      }
      roomId = res.Value;
    }

    return Result<string>.Success(roomId);
  }
}

