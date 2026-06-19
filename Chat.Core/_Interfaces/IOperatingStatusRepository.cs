using Chat.Core._ValueObjects;

namespace Chat.Core._Interfaces;

public interface IOperatingStatusRepository
{
  Task SetUserOnlineAsync(UserId userId, string connectionId, CancellationToken ct = default);
  Task SetUserOfflineAsync(UserId userId, string connectionId, CancellationToken ct = default);
  Task<List<string>> GetUserConnectionsAsync(UserId userId, CancellationToken ct = default);
  Task<bool> IsUserOnlineAsync(UserId userId, CancellationToken ct = default);
}

