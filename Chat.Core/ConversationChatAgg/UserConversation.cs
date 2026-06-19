using Chat.Core._ValueObjects;
using Chat.Core.ConversationChatAgg.VO;
using Chat.Core.MessageAgg.VO;

namespace Chat.Core.ConversationChatAgg;

public class UserConversation
{
  public Guid           Id             { get; private set; }
  public UserId         UserId         { get; private set; }
  public UserId         PartnerId      { get; private set; }
  public ConversationId ConversationId { get; private set; }
  public MessageId?     LastReadMessageId { get; private set; }

  public ICollection<RoomMessage>? RoomUserConversation { get; private set; }

  private UserConversation() { }

  public static UserConversation Create(UserId userId, UserId partnerId, ConversationId conversationId)
      => new()
      {
        Id             = Guid.NewGuid(),
        UserId         = userId,
        PartnerId      = partnerId,
        ConversationId = conversationId,
        RoomUserConversation = new HashSet<RoomMessage>()
      };

  public void UpdateLastRead(MessageId messageId) => LastReadMessageId = messageId;
}

