using Microsoft.AspNetCore.SignalR;


using Chat.Infrastructure.Hubs;

namespace Chat.Infrastructure._Services;

public class ChatHubService : IChatHubService
{
  private readonly IHubContext<ChatHub> _hubContext;

  public ChatHubService(IHubContext<ChatHub> hubContext)
  {
    _hubContext = hubContext;
  }

  public async Task SendToUserAsync(UserId userId, string eventName, object payload, CancellationToken ct = default)
  {
    await _hubContext.Clients.User(userId.Value.ToString()).SendAsync(eventName, payload, ct);
  }

  public async Task SendToUsersAsync(IReadOnlyList<UserId> userIds, string eventName, object payload, CancellationToken ct = default)
  {
    if (userIds.Count == 0) return;
    var userIdsStrings = userIds.Select(id => id.Value.ToString()).ToList();
    await _hubContext.Clients.Users(userIdsStrings).SendAsync(eventName, payload, ct);
  }

  public async Task SendToGroupAsync(string groupName, string eventName, object payload, CancellationToken ct = default)
  {
    await _hubContext.Clients.Group(groupName).SendAsync(eventName, payload, ct);
  }
}

