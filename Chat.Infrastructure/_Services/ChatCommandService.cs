using Chat.Core._ValueObjects;
using Chat.Core.ConversationChatAgg;
using Chat.Core.ConversationChatAgg.VO;
using Chat.Core.MessageAgg.VO;
using Chat.Infrastructure.Data.Context;
using Chat.UseCases.ChatApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ardalis.Result;

namespace Chat.Infrastructure._Services;

public class ChatCommandService(ChatDbContext dbContext, ILogger<ChatCommandService> logger) : IChatCommandService
{
  private readonly ChatDbContext _dbContext = dbContext;
  private readonly ILogger<ChatCommandService> _logger = logger;

  public async Task<Result<string>> CreateRoomMessageAsync(Guid myId, Guid partnerId, CancellationToken ct = default)
  {
    var myUserId = UserId.From(myId);
    var partnerUserId = UserId.From(partnerId);

    await using var tran = await _dbContext.Database.BeginTransactionAsync(ct);
    try
    {
      var converChat = ConversationChat.Create();
      var user1A = UserConversation.Create(myUserId, partnerUserId, converChat.Id);
      var user2A = UserConversation.Create(partnerUserId, myUserId, converChat.Id);

      await _dbContext.ConversationChats.AddAsync(converChat, ct);
      await _dbContext.UserConversations.AddAsync(user1A, ct);
      await _dbContext.UserConversations.AddAsync(user2A, ct);

      await _dbContext.RoomMessages.AddAsync(RoomMessage.CreateForConversation(myUserId, converChat.Id, user1A.Id), ct);
      await _dbContext.RoomMessages.AddAsync(RoomMessage.CreateForGroup(partnerUserId, converChat.Id.Value, user2A.Id), ct); // Map for legacy compatibility
      
      await _dbContext.SaveChangesAsync(ct);
      await tran.CommitAsync(ct);
      return Result<string>.Success(converChat.Id.Value.ToString());
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Lỗi khi tạo phòng chat mới giữa {MyId} và {PartnerId}", myId, partnerId);
      await tran.RollbackAsync(ct);
      return Result<string>.Error("Đã xảy ra lỗi khi tạo phòng chat");
    }
  }
}
