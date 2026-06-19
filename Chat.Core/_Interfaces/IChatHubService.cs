using Chat.Core._ValueObjects;

namespace Chat.Core._Interfaces;

public interface IChatHubService
{
  Task SendToUserAsync(UserId userId, string eventName, object payload, CancellationToken ct = default);
  Task SendToUsersAsync(IReadOnlyList<UserId> userIds, string eventName, object payload, CancellationToken ct = default);
  Task SendToGroupAsync(string groupName, string eventName, object payload, CancellationToken ct = default);
}

public static class ChatHubEvents
{
  public const string MessageReceived = "MessageReceived";
  public const string MessageLiked = "MessageLiked";
  public const string UserStatusChanged = "UserStatusChanged";
}

