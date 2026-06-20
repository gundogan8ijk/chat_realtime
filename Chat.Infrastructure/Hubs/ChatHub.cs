using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Chat.UseCases.ChatApp;
using Chat.Infrastructure._Services.Kafka;
using Chat.Infrastructure.Serializers;
using StackExchange.Redis;
using Chat.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Hubs;

public interface IChatClient
{
  Task ReceiveMessage(string hubType, MessageLiteTextDto msg);
  Task ReceiveNotification(string user, string message);
}

[Authorize]
public class ChatHub : Hub<IChatClient>
{
  private readonly IOperatingStatusRepository _repoOnline;
  private readonly KafkaProducerService _kafkaProducer;
  private readonly IConfiguration _configuration;
  private readonly IMessageRepository _repoMsg;
  private readonly ILogger<ChatHub> _logger;
  private readonly IDatabase _redisUserDb;
  private readonly ChatDbContext _dbContext;

  public ChatHub(
      IOperatingStatusRepository repoOnline,
      KafkaProducerService kafkaProducer,
      IConfiguration configuration,
      IMessageRepository repoMsg,
      IConnectionMultiplexer redis,
      ChatDbContext dbContext,
      ILogger<ChatHub> logger)
  {
    _repoOnline = repoOnline;
    _kafkaProducer = kafkaProducer;
    _configuration = configuration;
    _repoMsg = repoMsg;
    _logger = logger;
    _dbContext = dbContext;
    var stackUserIndex = int.Parse(_configuration["Redis:stackUser"] ?? "1");
    _redisUserDb = redis.GetDatabase(stackUserIndex);
  }

  public override async Task OnConnectedAsync()
  {
    var userIdString = Context.UserIdentifier;
    if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userIdGuid))
    {
      _logger.LogWarning("Không thể xác định UserId khi người dùng kết nối SignalR");
      Context.Abort();
      return;
    }

    var userId = UserId.From(userIdGuid);
    var connectionId = Context.ConnectionId;

    await _repoOnline.SetUserOnlineAsync(userId, connectionId);

    // Xử lý gửi các tin nhắn offline chưa đọc cho user vừa online trở lại
    var offlineMsgs = await _repoMsg.GetOfflineMessagesAsync(userId);
    if (offlineMsgs.Any())
    {
      foreach (var msgBytes in offlineMsgs)
      {
        try
        {
          var msgDto = ProtobufSerializer.Deserialize<MessageLiteTextDto>(msgBytes);
          await Clients.Caller.ReceiveMessage("MsgSingle", msgDto);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Lỗi khi deserialize và gửi tin nhắn offline");
        }
      }
      await _repoMsg.ClearOfflineMessagesAsync(userId);
    }

    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    var userIdString = Context.UserIdentifier;
    if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userIdGuid))
    {
      var userId = UserId.From(userIdGuid);
      var connectionId = Context.ConnectionId;
      await _repoOnline.SetUserOfflineAsync(userId, connectionId);
    }

    await base.OnDisconnectedAsync(exception);
  }

  public async Task SendTextMessageToUser(
      string partnerIdString, 
      string msgContent, 
      string roomIdDbString, 
      string fakeId, 
      string? replyMessageId, 
      string? replyMsgContent)
  {
    var myIdString = Context.UserIdentifier!;
    if (!Guid.TryParse(myIdString, out var myGuid))
    {
      _logger.LogWarning("Không thể xác định myId");
      return;
    }
    if (!Guid.TryParse(partnerIdString, out var partnerGuid))
    {
      _logger.LogWarning("partnerIdString không đúng định dạng Guid: {PartnerId}", partnerIdString);
      return;
    }
    if (!Guid.TryParse(roomIdDbString, out var roomGuid))
    {
      _logger.LogWarning("roomIdDbString không đúng định dạng Guid: {RoomId}", roomIdDbString);
      return;
    }

    var myId = UserId.From(myGuid);
    var partnerId = UserId.From(partnerGuid);
    var roomIdDb = UserId.From(roomGuid); // In legacy structure, roomId is mapped as ReceiverId (UserId VO)

    string fullname = "User";
    try
    {
      var cachedProfile = await _redisUserDb.HashGetAllAsync(myIdString);
      if (cachedProfile.Length > 0)
      {
        var dict = cachedProfile.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
        var lastName = dict.GetValueOrDefault("lastName") ?? "";
        var firstName = dict.GetValueOrDefault("firstName") ?? "";
        fullname = $"{lastName} {firstName}".Trim();
      }
      else
      {
        var dbProfile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.Id == myId, Context.ConnectionAborted);
        if (dbProfile != null)
        {
          fullname = $"{dbProfile.LastName.Value} {dbProfile.FirstName.Value}".Trim();
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Lỗi khi lấy fullname từ cache/db trong ChatHub");
      fullname = Context.User?.Identity?.Name ?? "User";
    }

    MessageId? parentMsgId = Guid.TryParse(replyMessageId, out var parsedReplyId) 
        ? MessageId.From(parsedReplyId) 
        : null;

    // Tạo domain entity Message
    var msgDb = Message.Create(
        senderUserId: myId,
        receiverId: roomIdDb,
        body: MessageContent.From(msgContent),
        type: MessageType.Text,
        parentMessageId: parentMsgId);

    var msgDto = new MessageLiteTextDto(
        msgDb.Id.Value, 
        myId.Value, 
        fullname, 
        msgContent, 
        roomIdDb.Value, 
        replyMessageId, 
        replyMsgContent);

    // Gửi phản hồi lại cho người gửi kèm realId / fakeId
    await Clients.Caller.ReceiveMessage("myIdMess", new MessageLiteTextDto(msgDb.Id.Value, fakeId, roomIdDb.Value));

    // Kiểm tra xem đối tác có online không
    var isOnline = await _repoOnline.IsUserOnlineAsync(partnerId);
    if (isOnline)
    {
      // Sử dụng cơ chế native của SignalR để gửi đến toàn bộ các connections của user
      await Clients.User(partnerIdString).ReceiveMessage("MsgSingle", msgDto);
    }
    else
    {
      // Người nhận offline -> lưu tin nhắn vào Redis offline cache
      var serializedMsg = ProtobufSerializer.Serialize(msgDto);
      await _repoMsg.SaveOfflineMessageAsync(partnerId, serializedMsg);
    }

    // Đẩy vào Kafka để xử lý ghi vào cơ sở dữ liệu sau (eventual consistency)
    var kafkaDto = new KafkaMessageDto
    {
      Id = msgDb.Id.Value,
      SenderUserId = msgDb.SenderUserId.Value,
      ReceiverId = msgDb.ReceiverId.Value,
      MessageBody = msgDb.MessageBody?.Value,
      CreateDate = msgDb.CreateDate,
      MessageType = msgDb.MessageType.Name,
      ParentMessageId = msgDb.ParentMessageId?.Value
    };

    var kafkaMsg = _kafkaProducer.CreateMsg(kafkaDto, roomIdDb.Value.ToString());
    var topic = _configuration["Kafka:Topic1"] ?? "chat-messages";
    await _kafkaProducer.AddProduceAsync(topic, kafkaMsg, CancellationToken.None);
  }
}

