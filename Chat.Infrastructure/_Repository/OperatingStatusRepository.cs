

using StackExchange.Redis;

namespace Chat.Infrastructure._Repository;

public class OperatingStatusRepository : IOperatingStatusRepository
{
  private readonly IDatabase _db;
  private readonly IConnectionMultiplexer _redis;
  private readonly IConfiguration _configuration;

  public OperatingStatusRepository(IConnectionMultiplexer redis, IConfiguration configuration)
  {
    _redis = redis;
    _configuration = configuration;
    var dbIndex = _configuration["redis:stackUser"] != null 
        ? int.Parse(_configuration["redis:stackUser"]!) 
        : 0;
    _db = _redis.GetDatabase(dbIndex);
  }

  public async Task SetUserOnlineAsync(UserId userId, string connectionId, CancellationToken ct = default)
  {
    var key = $"user:{userId.Value}";
    await _db.HashSetAsync(key, new HashEntry[] { new HashEntry("statusOnline", "1") });
    
    var connKey = $"connections:{userId.Value}";
    await _db.SetAddAsync(connKey, connectionId);
  }

  public async Task SetUserOfflineAsync(UserId userId, string connectionId, CancellationToken ct = default)
  {
    var connKey = $"connections:{userId.Value}";
    await _db.SetRemoveAsync(connKey, connectionId);

    var activeConnectionsCount = await _db.SetLengthAsync(connKey);
    if (activeConnectionsCount == 0)
    {
      var key = $"user:{userId.Value}";
      var userOffline = new HashEntry[]
      {
        new HashEntry("statusOnline", "0"),
        new HashEntry("lastTimeOnline", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
      };
      await _db.HashSetAsync(key, userOffline);
    }
  }

  public async Task<List<string>> GetUserConnectionsAsync(UserId userId, CancellationToken ct = default)
  {
    var connKey = $"connections:{userId.Value}";
    var members = await _db.SetMembersAsync(connKey);
    return members.Select(m => m.ToString()).ToList();
  }

  public async Task<bool> IsUserOnlineAsync(UserId userId, CancellationToken ct = default)
  {
    var key = $"user:{userId.Value}";
    var val = await _db.HashGetAsync(key, "statusOnline");
    return val.HasValue && val.ToString() == "1";
  }
}

