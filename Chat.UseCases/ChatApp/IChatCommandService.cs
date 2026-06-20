namespace Chat.UseCases.ChatApp;

public interface IChatCommandService
{
  Task<Result<string>> CreateRoomMessageAsync(Guid myId, Guid partnerId, CancellationToken ct = default);
}
