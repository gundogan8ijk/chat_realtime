using Chat.Core._ValueObjects;
using Chat.Core.ConversationChatAgg.VO;

namespace Chat.Core.ConversationChatAgg;

public class RoomMessage
{
  public RoomId         RoomId             { get; private set; }
  public UserId         UserId             { get; private set; }

  // ── 1-1 chat ──
  public ConversationId? ConversationChatId { get; private set; }
  public Guid?           UserConversationId { get; private set; }

  // ── Group chat ──
  public Guid? GroupChatId { get; private set; }
  public Guid? UserGroupId { get; private set; }

  private RoomMessage() { }

  public static RoomMessage CreateForConversation(
      UserId userId, ConversationId conversationChatId, Guid userConversationId)
      => new()
      {
        RoomId             = RoomId.From(Guid.NewGuid()),
        UserId             = userId,
        ConversationChatId = conversationChatId,
        UserConversationId = userConversationId
      };

  public static RoomMessage CreateForGroup(UserId userId, Guid groupChatId, Guid userGroupId)
      => new()
      {
        RoomId      = RoomId.From(Guid.NewGuid()),
        UserId      = userId,
        GroupChatId = groupChatId,
        UserGroupId = userGroupId
      };
}

