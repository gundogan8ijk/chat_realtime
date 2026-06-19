using ProtoBuf;

namespace Chat.UseCases.ChatApp;

[ProtoContract]
public class MessageLiteTextDto
{
  public MessageLiteTextDto()
  {
  }

  [ProtoMember(1)] public Guid SenderId { get; set; }
  [ProtoMember(2)] public string FullName { get; set; } = string.Empty;
  [ProtoMember(3)] public string MessContent { get; set; } = string.Empty;
  [ProtoMember(4)] public Guid MessId { get; set; } = Guid.NewGuid();
  [ProtoMember(5)] public Guid RoomId { get; set; }
  [ProtoMember(6)] public string DateTimeSend { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
  [ProtoMember(7)] public string? NameGroup { get; set; }
  [ProtoMember(8)] public string TextType { get; set; } = "Text";
  [ProtoMember(9)] public string? ReplyMessageId { get; set; }
  [ProtoMember(10)] public string? ReplyMsgContent { get; set; }

  public MessageLiteTextDto(Guid realId, string fakeId, Guid roomId)
  {
    RoomId = roomId;
    MessId = realId;
    DateTimeSend = fakeId;
  }

  public MessageLiteTextDto(Guid messId, Guid senderId, string fullName, string messContent, Guid roomId, string? replyMessageId, string? replyMsgContent)
  {
    MessId = messId;
    SenderId = senderId;
    FullName = fullName;
    MessContent = messContent;
    RoomId = roomId;
    ReplyMessageId = replyMessageId;
    ReplyMsgContent = replyMsgContent;
  }
}

