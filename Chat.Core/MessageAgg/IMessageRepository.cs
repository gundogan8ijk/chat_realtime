using Chat.Core._ValueObjects;
using Chat.Core.MessageAgg.VO;
using Chat.Core.ConversationChatAgg;
using Chat.Core.ConversationChatAgg.VO;

namespace Chat.Core.MessageAgg;

public interface IMessageRepository : IRepository<Message>
{
  Task<List<Message>> GetConversationMessagesAsync(
      ConversationId conversationId,
      int            page            = 1,
      int            pageSize        = 50,
      CancellationToken ct           = default);

  Task<List<Message>> GetPendingByReceiverAsync(
      UserId receiverId, CancellationToken ct = default);

  Task SaveBatchAsync(
      IEnumerable<Message> listMess,
      IEnumerable<ConversationChat> listCon,
      IEnumerable<UserConversation> listUserCon,
      CancellationToken ct = default);

  Task SaveOfflineMessageAsync(UserId receiverId, byte[] msg, CancellationToken ct = default);
  Task<List<byte[]>> GetOfflineMessagesAsync(UserId receiverId, CancellationToken ct = default);
  Task ClearOfflineMessagesAsync(UserId receiverId, CancellationToken ct = default);
}

