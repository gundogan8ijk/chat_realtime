using Chat.Core._ValueObjects;
using Chat.Core.GroupChatAgg.VO;
using Chat.Core.MessageAgg.VO;

namespace Chat.Core.GroupChatAgg;

public class UserGroup
{
  public UserGroupId  Id          { get; private set; }
  public GroupChatId  GroupChatId { get; private set; }
  public UserId       UserId      { get; private set; }
  public bool         IsAdmin     { get; private set; } = false;
  public DateTime     JoinedAt    { get; private set; } = DateTime.UtcNow;
  public MessageId?   LastReadMessageId { get; private set; }

  private UserGroup() { }

  public static UserGroup Create(GroupChatId groupChatId, UserId userId, bool isAdmin = false)
      => new()
      {
        Id          = UserGroupId.From(Guid.NewGuid()),
        GroupChatId = groupChatId,
        UserId      = userId,
        IsAdmin     = isAdmin,
        JoinedAt    = DateTime.UtcNow
      };

  public void MakeAdmin()           => IsAdmin = true;
  public void UpdateLastRead(MessageId messageId) => LastReadMessageId = messageId;
}

