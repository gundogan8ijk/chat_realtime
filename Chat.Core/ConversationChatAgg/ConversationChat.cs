using Chat.Core._ValueObjects;
using Chat.Core.ConversationChatAgg.VO;
using Chat.Core.MessageAgg.VO;

namespace Chat.Core.ConversationChatAgg;

public class ConversationChat : EntityBase<ConversationChat, ConversationId>, IAggregateRoot
{
  public MessageId? LastSentMessageId { get; private set; }
  public UserId     UserBlockId       { get; private set; }

  private readonly HashSet<UserConversation> _userConversations = [];
  public IReadOnlyCollection<UserConversation> UserConversations => _userConversations.AsReadOnly();

  private ConversationChat() { }

  public static ConversationChat Create()
      => new()
      {
        Id                = ConversationId.From(Guid.NewGuid())
      };

  public void AddParticipant(UserId userId, UserId partnerId)
  {
    var participant = UserConversation.Create(userId, partnerId, Id);
    _userConversations.Add(participant);
  }

  public void UpdateLastMessage(MessageId messageId) => LastSentMessageId = messageId;
}

