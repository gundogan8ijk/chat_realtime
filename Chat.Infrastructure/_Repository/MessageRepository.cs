using Chat.Infrastructure.Data.Context;
using StackExchange.Redis;

namespace Chat.Infrastructure._Repository;

public class MessageRepository : EfRepository<Message>, IMessageRepository
{
  private readonly ChatDbContext _dbContext;
  private readonly IDatabase _redisDb;
  private readonly ILogger<MessageRepository> _logger;

  public MessageRepository(
      ChatDbContext dbContext,
      IConnectionMultiplexer redis,
      IConfiguration configuration,
      ILogger<MessageRepository> logger) : base(dbContext)
  {
    _dbContext = dbContext;
    _logger = logger;
    
    var dbIndex = configuration["redis:stackMessage"] != null 
        ? int.Parse(configuration["redis:stackMessage"]!) 
        : 1; // Default to database 1 for messages
    _redisDb = redis.GetDatabase(dbIndex);
  }

  public async Task<List<Message>> GetConversationMessagesAsync(
      ConversationId conversationId,
      int page = 1,
      int pageSize = 50,
      CancellationToken ct = default)
  {
    // Wait, how does Message relate to ConversationChat in EF?
    // RoomMessage or UserConversation maps UserId and ConversationId. 
    // And ReceiverId or some columns might match. But wait!
    // In our ChatAgg, a Message has ReceiverId (UserId) which in 1-1 chat could be the partner's UserId.
    // Wait, let's query messages in a conversation: we can get user conversation participants, 
    // or select messages where SenderUserId & ReceiverId match the conversation.
    // Or we can map ConversationId directly if RoomMessage has it.
    // But since the legacy project queries messages by ReceiverId (which acts as roomId/conversationId),
    // let's check how the legacy code queries messages!
    // Let's search test_v1_chat for query of messages.
    return await _dbContext.Messages
        .Where(m => m.ReceiverId == UserId.From(conversationId.Value)) // In legacy, RoomId is passed as ReceiverId
        .OrderByDescending(m => m.CreateDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .OrderBy(m => m.CreateDate) // chronological order for display
        .ToListAsync(ct);
  }

  public async Task<List<Message>> GetPendingByReceiverAsync(
      UserId receiverId, CancellationToken ct = default)
  {
    return await _dbContext.Messages
        .Where(m => m.ReceiverId == receiverId && m.Status != DeliveryStatus.Read)
        .ToListAsync(ct);
  }

  public async Task SaveBatchAsync(
      IEnumerable<Message> listMess,
      IEnumerable<ConversationUpdateDto> listCon,
      IEnumerable<UserConversationUpdateDto> listUserCon,
      CancellationToken ct = default)
  {
    using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
    try
    {
      if (listMess.Any())
      {
        await _dbContext.Messages.AddRangeAsync(listMess, ct);
        await _dbContext.SaveChangesAsync(ct);
      }

      foreach (var con in listCon)
      {
        await _dbContext.ConversationChats
            .Where(x => x.Id == con.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastSentMessageId, con.LastSentMessageId), ct);
      }

      foreach (var uc in listUserCon)
      {
        await _dbContext.UserConversations
            .Where(x => x.UserId == uc.UserId && x.ConversationId == uc.ConversationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastReadMessageId, uc.LastReadMessageId), ct);
      }

      await transaction.CommitAsync(ct);
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync(ct);
      _logger.LogError(ex, "Lỗi khi lưu batch tin nhắn vào database");
      throw;
    }
  }

  public async Task SaveOfflineMessageAsync(UserId receiverId, byte[] msg, CancellationToken ct = default)
  {
    try
    {
      var key = $"offline:{receiverId.Value}";
      await _redisDb.ListRightPushAsync(key, msg);
      // Keep only last 500 offline messages to prevent abuse
      await _redisDb.ListTrimAsync(key, -500, -1);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Lỗi khi lưu tin nhắn offline vào Redis cho user: {UserId}", receiverId.Value);
    }
  }

  public async Task<List<byte[]>> GetOfflineMessagesAsync(UserId receiverId, CancellationToken ct = default)
  {
    var key = $"offline:{receiverId.Value}";
    var values = await _redisDb.ListRangeAsync(key);
    return values.Select(v => (byte[])v!).ToList();
  }

  public async Task ClearOfflineMessagesAsync(UserId receiverId, CancellationToken ct = default)
  {
    var key = $"offline:{receiverId.Value}";
    await _redisDb.KeyDeleteAsync(key);
  }
}

