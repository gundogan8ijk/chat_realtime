using Chat.Core._ValueObjects;
using Chat.Core.GroupChatAgg.VO;
using Chat.Core.MessageAgg.VO;

namespace Chat.Core.GroupChatAgg;

public class GroupChat : EntityBase<GroupChat, GroupChatId>, IAggregateRoot
{
  public GroupName  Name              { get; private set; }
  public DateTime   CreateDate        { get; private set; } = DateTime.UtcNow;
  public bool       IsActive          { get; private set; } = true;
  public AvatarUrl? AvatarUrl         { get; private set; }
  public bool       IsPrivate         { get; private set; } = true;
  public MessageId? LastSentMessageId { get; private set; }

  private readonly HashSet<UserGroup> _userGroups = [];
  public IReadOnlyCollection<UserGroup> UserGroups => _userGroups.AsReadOnly();

  private GroupChat() { }

  public static GroupChat Create(GroupName name, AvatarUrl? avatarUrl = null, bool isPrivate = true)
      => new()
      {
        Id         = GroupChatId.From(Guid.NewGuid()),
        Name       = name,
        AvatarUrl  = avatarUrl,
        IsPrivate  = isPrivate,
        CreateDate = DateTime.UtcNow
      };

  public void UpdateLastMessage(MessageId messageId) => LastSentMessageId = messageId;
  public void Rename(GroupName newName)              => Name = newName;
  public void Deactivate()                           => IsActive = false;
}

