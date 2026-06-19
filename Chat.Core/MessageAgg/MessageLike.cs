using Chat.Core._ValueObjects;
using Chat.Core.MessageAgg.Enums;
using Chat.Core.MessageAgg.VO;

namespace Chat.Core.MessageAgg;

public class MessageLike
{
  public MessageId MessageId    { get; private set; }
  public UserId    UserId       { get; private set; }
  public DateTime  DateTimeLike { get; private set; } = DateTime.UtcNow;
  public LikeType  LikeType     { get; private set; } = LikeType.Like;
  public bool      IsActive     { get; private set; } = true;

  private MessageLike() { }

  public static MessageLike Create(MessageId messageId, UserId userId, LikeType likeType)
      => new()
      {
        MessageId    = messageId,
        UserId       = userId,
        LikeType     = likeType,
        DateTimeLike = DateTime.UtcNow,
        IsActive     = true
      };

  public void Toggle() => IsActive = !IsActive;
  public void Change(LikeType newType) => LikeType = newType;
}

