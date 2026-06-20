using Chat.Core._ValueObjects;
using Chat.Core.MessageAgg.Enums;
using Chat.Core.MessageAgg.Events;
using Chat.Core.MessageAgg.VO;

namespace Chat.Core.MessageAgg;

public class Message : EntityBase<Message, MessageId>, IAggregateRoot
{
  public UserId         SenderUserId    { get; private set; }
  public UserId         ReceiverId      { get; private set; }
  public MessageContent? MessageBody    { get; private set; }
  public DateTime        CreateDate     { get; private set; } = DateTime.UtcNow;
  public MessageType     MessageType    { get; private set; } = MessageType.Text;
  public MessageId?      ParentMessageId { get; private set; }

  public DeliveryStatus Status   { get; private set; } = DeliveryStatus.Sending;
  public bool           IsDelete { get; private set; } = false;

  // ── Navigation ───────────────────────────────────────────────────
  public Message?                  ParentMessage { get; private set; }
  private readonly HashSet<Message> _replies = [];
  public IReadOnlyCollection<Message> Replies => _replies.AsReadOnly();

  private readonly HashSet<MessageLike> _messageLikes = [];
  public IReadOnlyCollection<MessageLike> MessageLikes => _messageLikes.AsReadOnly();

  private Message() { }

  public static Message Create(
      UserId          senderUserId,
      UserId          receiverId,
      MessageContent? body            = null,
      MessageType?    type            = null,
      MessageId?      parentMessageId = null,
      MessageId?      id              = null)
  {
    var msg = new Message
    {
      Id              = id ?? MessageId.From(Guid.NewGuid()),
      SenderUserId    = senderUserId,
      ReceiverId      = receiverId,
      MessageBody     = body,
      MessageType     = type ?? MessageType.Text,
      ParentMessageId = parentMessageId,
      CreateDate      = DateTime.UtcNow
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

