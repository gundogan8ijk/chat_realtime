namespace Chat.UseCases.ChatApp;

public interface IChatQueryService
{
  Task<FrameDialoChatDto?> GetFrameRoomChatAsync(Guid myId, Guid roomId, int pageSize, int pageNumber, CancellationToken ct = default);
  Task<string?> GetIdConversationChatAsync(Guid myId, Guid partnerId, CancellationToken ct = default);
  Task<Result<string>> CreateRoomMessageAsync(Guid myId, Guid partnerId, CancellationToken ct = default);
  Task<List<ItemChatDto>> GetListItemChatActiveAsync(Guid myId, int pageSize, int pageNumber, CancellationToken ct = default);
  Task<UserSearchDto?> SearchUserByEmailAsync(string email, string myEmail, Guid myId, CancellationToken ct = default);
  Task<Result<UserDto>> GetMyProfileUserAsync(Guid myId, CancellationToken ct = default);
}

