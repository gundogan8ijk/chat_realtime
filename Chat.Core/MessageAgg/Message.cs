using Chat.Core._ValueObjects;
using Chat.Core.MessageAgg.Enums;
using Chat.Core.MessageAgg.Events;
using Chat.Core.MessageAgg.VO;
using ProtoBuf;

namespace Chat.Core.MessageAgg;

[ProtoContract]
public class Message : EntityBase<Message, MessageId>, IAggregateRoot
{
  [ProtoMember(1)] public UserId         SenderUserId    { get; private set; }
  [ProtoMember(2)] public UserId         ReceiverId      { get; private set; }
  [ProtoMember(3)] public MessageContent? MessageBody    { get; private set; }
  [ProtoMember(4)] public DateTime        CreateDate     { get; private set; } = DateTime.UtcNow;
  [ProtoMember(5)] public MessageType     MessageType    { get; private set; } = MessageType.Text;
  [ProtoMember(6)] public MessageId?      ParentMessageId { get; private set; }

  public DeliveryStatus Status   { get; private set; } = DeliveryStatus.Sending;
  public bool           IsDelete { get; private set; } = false;

  // ── Navigation ───────────────────────────────────────────────────
  public Message?                  ParentMessage { get; private set; }
  public ICollection<Message>?     Replies       { get; private set; }
  public ICollection<MessageLike>? MessageLikes  { get; private set; }

  private Message() { }

  public static Message Create(
      UserId          senderUserId,
      UserId          receiverId,
      MessageContent? body            = null,
      MessageType?    type            = null,
      MessageId?      parentMessageId = null)
  {
    var msg = new Message
    {
      Id              = MessageId.From(Guid.NewGuid()),
      SenderUserId    = senderUserId,
      ReceiverId      = receiverId,
      MessageBody     = body,
      MessageType     = type ?? MessageType.Text,
      ParentMessageId = parentMessageId,
      CreateDate      = DateTime.UtcNow,
      Replies         = new HashSet<Message>(),
      MessageLikes    = new HashSet<MessageLike>()
    };

    msg.RegisterDomainEvent(new MessageSentEvent(msg));
    return msg;
  }

  public void MarkAsRead()
  {
    if (Status == DeliveryStatus.Read) return;
    Status = DeliveryStatus.Read;
  }

  public void SoftDelete()
  {
    IsDelete = true;
    Status   = DeliveryStatus.IsDeleted;
  }
}

