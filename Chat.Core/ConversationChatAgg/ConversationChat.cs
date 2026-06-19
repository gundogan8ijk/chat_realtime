using Chat.Core._ValueObjects;
using Chat.Core.ConversationChatAgg.VO;
using Chat.Core.MessageAgg.VO;

namespace Chat.Core.ConversationChatAgg;

public class ConversationChat : EntityBase<ConversationChat, ConversationId>, IAggregateRoot
{
  public MessageId? LastSentMessageId { get; private set; }
  public UserId     UserBlockId       { get; private set; }

  public ICollection<UserConversation>? UserConversations { get; private set; }

  private ConversationChat() { }

  public static ConversationChat Create()
      => new()
      {
        Id                = ConversationId.From(Guid.NewGuid()),
        UserConversations = new HashSet<UserConversation>()
      };

  public void UpdateLastMessage(MessageId messageId) => LastSentMessageId = messageId;
}

