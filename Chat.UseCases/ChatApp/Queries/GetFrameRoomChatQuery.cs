using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Queries;

public record GetFrameRoomChatQuery(
    Guid MyId, 
    Guid RoomId, 
    int PageSize, 
    int PageNumber) : IQuery<Result<FrameDialoChatDto>>;

