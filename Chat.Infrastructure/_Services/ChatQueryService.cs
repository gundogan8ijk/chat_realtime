using Microsoft.AspNetCore.Identity;
using Ardalis.Result;
using Chat.Core.AccountAgg;





using Chat.UseCases.ChatApp;
using Chat.Infrastructure.Data.Context;

using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace Chat.Infrastructure._Services;

public class ChatQueryService : IChatQueryService
{
  private readonly ChatDbContext _dbContext;
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IDatabase _redisDb;
  private readonly ILogger<ChatQueryService> _logger;

  public ChatQueryService(
      ChatDbContext dbContext,
      UserManager<ApplicationUser> userManager,
      IConnectionMultiplexer redis,
      IConfiguration configuration,
      ILogger<ChatQueryService> logger)
  {
    _dbContext = dbContext;
    _userManager = userManager;
    var stackUserIndex = int.Parse(configuration["Redis:stackUser"] ?? "1");
    _redisDb = redis.GetDatabase(stackUserIndex);
    _logger = logger;
  }

  public async Task<FrameDialoChatDto?> GetFrameRoomChatAsync(
      Guid myId, 
      Guid roomId, 
      int pageSize, 
      int pageNumber, 
      CancellationToken ct = default)
  {
    var myUserId = UserId.From(myId);
    var conversationId = ConversationId.From(roomId);

    // 1. Kiểm tra sự tồn tại của UserConversation của tôi trong phòng này
    var myUserConv = await _dbContext.UserConversations.AsNoTracking()
        .FirstOrDefaultAsync(x => x.UserId == myUserId && x.ConversationId == conversationId, ct);

    if (myUserConv == null)
    {
      return null;
    }

    // 2. Tìm đối tác trong cuộc hội thoại (UserId != myId)
    var partnerConv = await _dbContext.UserConversations.AsNoTracking()
        .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId != myUserId, ct);

    if (partnerConv == null)
    {
      return null;
    }

    var partnerUser = await _dbContext.Users.AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == partnerConv.UserId.Value, ct);

    string partnerName = partnerUser?.UserName ?? "User";
    string? avatarUrl = null; // Identity ApplicationUser không có AvatarUrl, nhưng ta trả về null

    // 3. Kiểm tra xem có phải bạn bè không
    var isFriend = await _dbContext.Friendships.AsNoTracking()
        .AnyAsync(y => 
            ((y.UserId_A == myUserId && y.UserId_B == partnerConv.UserId) || 
             (y.UserId_A == partnerConv.UserId && y.UserId_B == myUserId)) && 
            y.Status == FriendStatus.Connect, ct);

    var titleRoom = new TitleRoomDialoDto(
        partnerConv.UserId.Value.ToString(),
        partnerName,
        isFriend,
        avatarUrl,
        roomId.ToString(),
        myUserConv.LastReadMessageId?.Value);

    // 4. Lấy danh sách tin nhắn
    // Trong legacy, ReceiverId của tin nhắn chính là roomId (ID cuộc hội thoại)
    var roomIdVo = UserId.From(roomId);
    
    var messages = await _dbContext.Messages.AsNoTracking()
        .Where(x => x.ReceiverId == roomIdVo && !x.IsDelete)
        .OrderByDescending(x => x.CreateDate)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Include(m => m.MessageLikes)
        .Include(m => m.ParentMessage)
        .ToListAsync(ct);

    var listMessDto = new List<ItemMessDialogueDto>();

    foreach (var x in messages)
    {
      var likesList = new List<UserLikeMessDto>();
      if (x.MessageLikes != null)
      {
        foreach (var l in x.MessageLikes.Where(y => y.IsActive))
        {
          string nameLike = "Người dùng";
          if (l.UserId == myUserId)
          {
            nameLike = "bạn";
          }
          else
          {
            var uLike = await _dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == l.UserId.Value, ct);
            if (uLike != null)
            {
              nameLike = uLike.UserName ?? "Người dùng";
            }
          }
          likesList.Add(new UserLikeMessDto(nameLike, l.LikeType.Name));
        }
      }

      if (x.ParentMessageId == null)
      {
        listMessDto.Add(new ItemMessDialogueDto(
            x.Id.Value,
            x.SenderUserId == myUserId,
            x.MessageBody?.Value,
            x.MessageType.Name,
            x.CreateDate,
            likesList));
      }
      else
      {
        listMessDto.Add(new ItemMessDialogueDto(
            x.Id.Value,
            x.SenderUserId == myUserId,
            x.MessageBody?.Value,
            x.MessageType.Name,
            x.CreateDate,
            likesList,
            x.ParentMessageId.Value.Value,
            x.ParentMessage?.MessageBody?.Value));
      }
    }

    return new FrameDialoChatDto(titleRoom, listMessDto);
  }

  public async Task<string?> GetIdConversationChatAsync(Guid myId, Guid partnerId, CancellationToken ct = default)
  {
    var myUserId = UserId.From(myId);
    var partnerUserId = UserId.From(partnerId);

    var idConver = await _dbContext.UserConversations.AsNoTracking()
        .Where(x => x.UserId == myUserId && x.PartnerId == partnerUserId)
        .Select(x => x.ConversationId)
        .FirstOrDefaultAsync(ct);

    if (idConver.Value == Guid.Empty)
    {
      return null;
    }

    return idConver.Value.ToString();
  }

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

  public async Task<List<ItemChatDto>> GetListItemChatActiveAsync(
      Guid myId, 
      int pageSize, 
      int pageNumber, 
      CancellationToken ct = default)
  {
    var myUserId = UserId.From(myId);

    // Lấy danh sách RoomMessages thuộc về user hiện tại
    var rooms = await _dbContext.RoomMessages.AsNoTracking()
        .Where(x => x.UserId == myUserId && (x.GroupChatId != null || x.ConversationChatId != null))
        .ToListAsync(ct);

    var listActiveChats = new List<ItemChatDto>();

    foreach (var r in rooms)
    {
      if (r.ConversationChatId != null)
      {
        var conv = await _dbContext.ConversationChats.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == r.ConversationChatId, ct);

        if (conv == null || conv.LastSentMessageId == null)
        {
          continue;
        }

        var lastMsg = await _dbContext.Messages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == conv.LastSentMessageId, ct);

        if (lastMsg == null)
        {
          continue;
        }

        // Tìm UserConversation của tôi để check LastReadMessageId
        var myUserConv = await _dbContext.UserConversations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == myUserId && x.ConversationId == r.ConversationChatId, ct);

        // Tìm đối tác trong cuộc hội thoại
        var partnerConv = await _dbContext.UserConversations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ConversationId == r.ConversationChatId && x.UserId != myUserId, ct);

        if (partnerConv == null) continue;

        var partnerUser = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == partnerConv.UserId.Value, ct);

        if (partnerUser == null) continue;

        string partnerName = partnerUser.UserName ?? "User";
        string? avatarUrl = null;

        var senderUser = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == lastMsg.SenderUserId.Value, ct);
        string senderName = senderUser?.UserName ?? "User";

        bool isRead = lastMsg.SenderUserId == myUserId || 
                     (myUserConv != null && myUserConv.LastReadMessageId == conv.LastSentMessageId);

        listActiveChats.Add(new ItemChatDto(
            r.ConversationChatId.Value.ToString(),
            partnerName,
            avatarUrl,
            lastMsg.MessageBody?.Value,
            senderName,
            lastMsg.CreateDate,
            lastMsg.MessageType.Name,
            isRead));
      }
      else if (r.GroupChatId != null)
      {
        var group = await _dbContext.GroupChats.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == GroupChatId.From(r.GroupChatId.Value), ct);

        if (group == null) continue;

        if (group.LastSentMessageId != null)
        {
          var lastMsg = await _dbContext.Messages.AsNoTracking()
              .FirstOrDefaultAsync(m => m.Id == group.LastSentMessageId, ct);

          if (lastMsg != null)
          {
            var senderUser = await _dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == lastMsg.SenderUserId.Value, ct);
            string senderName = senderUser?.UserName ?? "User";

            // Tìm UserGroup của tôi để check LastReadMessageId
            var myUserGroup = await _dbContext.UserGroups.AsNoTracking()
                .FirstOrDefaultAsync(ug => ug.UserId == myUserId && ug.GroupChatId == group.Id, ct);

            bool isRead = lastMsg.SenderUserId == myUserId || 
                         (myUserGroup != null && myUserGroup.LastReadMessageId == group.LastSentMessageId);

            listActiveChats.Add(new ItemChatDto(
                group.Id.Value.ToString(),
                group.Name.Value,
                group.AvatarUrl?.Value,
                true,
                lastMsg.MessageBody?.Value,
                senderName,
                lastMsg.CreateDate,
                lastMsg.MessageType.Name,
                isRead));
          }
          else
          {
            listActiveChats.Add(new ItemChatDto(group.Id.Value.ToString(), group.Name.Value, group.AvatarUrl?.Value));
          }
        }
        else
        {
          listActiveChats.Add(new ItemChatDto(group.Id.Value.ToString(), group.Name.Value, group.AvatarUrl?.Value));
        }
      }
    }

    // Sắp xếp giảm dần theo thời gian tin nhắn mới nhất
    return listActiveChats.OrderByDescending(c => c.TimeSentMessLast).ToList();
  }

  public async Task<UserSearchDto?> SearchUserByEmailAsync(
      string email, 
      string myEmail, 
      Guid myId, 
      CancellationToken ct = default)
  {
    var targetUser = await _dbContext.Users.AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == email, ct);

    if (targetUser == null)
    {
      return null;
    }

    var myUserId = UserId.From(myId);
    var partnerUserId = UserId.From(targetUser.Id);

    var userDto = new UserDto(
        targetUser.Id.ToString(),
        null, // AvatarUrl
        null, // LastName
        targetUser.UserName, // FirstName / Name
        targetUser.Email ?? "",
        DateTime.UtcNow);

    if (email.Equals(myEmail, StringComparison.OrdinalIgnoreCase))
    {
      return new UserSearchDto(userDto, "isMe");
    }

    // Check relationship
    var friendship = await _dbContext.Friendships.AsNoTracking()
        .FirstOrDefaultAsync(y => 
            (y.UserId_A == myUserId && y.UserId_B == partnerUserId) || 
            (y.UserId_A == partnerUserId && y.UserId_B == myUserId), ct);

    string statusFriend = "noLink";

    if (friendship != null)
    {
      if (friendship.Status == FriendStatus.Connect)
      {
        statusFriend = "friend";
      }
      else if (friendship.Status == FriendStatus.APending)
      {
        statusFriend = friendship.UserId_A == myUserId ? "sentInvitations" : "receivedInvitation";
      }
      else if (friendship.Status == FriendStatus.BPending)
      {
        statusFriend = friendship.UserId_B == myUserId ? "sentInvitations" : "receivedInvitation";
      }
      else if (friendship.Status == FriendStatus.ABlocked || friendship.Status == FriendStatus.BBlocked)
      {
        return null; // Mối quan hệ bị chặn -> ẩn kết quả tìm kiếm
      }
    }

    return new UserSearchDto(userDto, statusFriend);
  }

  public async Task<Result<UserDto>> GetMyProfileUserAsync(Guid myId, CancellationToken ct = default)
  {
    var redisKey = myId.ToString();
    try
    {
      var cachedData = await _redisDb.HashGetAllAsync(redisKey);
      if (cachedData.Length > 0)
      {
        var dict = cachedData.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
        var email = dict.GetValueOrDefault("email") ?? string.Empty;
        var lastName = dict.GetValueOrDefault("lastName") ?? string.Empty;
        var firstName = dict.GetValueOrDefault("firstName") ?? string.Empty;
        var avatarUrl = dict.GetValueOrDefault("avatarURL");
        var createDateStr = dict.GetValueOrDefault("createDate") ?? "0";

        return Result.Success(new UserDto
        {
          UserId = myId.ToString(),
          Email = email,
          LastName = lastName,
          FirstName = firstName,
          AvatarURL = avatarUrl,
          CreateDate = createDateStr
        });
      }
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Lỗi khi đọc profile từ Redis, fallback sang Database");
    }

    var user = await _userManager.FindByIdAsync(myId.ToString());
    if (user == null)
    {
      return Result.Error("Người dùng không tồn tại");
    }

    var profile = await _dbContext.UserProfiles
        .FirstOrDefaultAsync(p => p.Id == UserId.From(myId), ct);

    var createDateMs = ((DateTimeOffset)(profile?.CreateDate ?? DateTime.UtcNow)).ToUnixTimeMilliseconds().ToString();

    return Result.Success(new UserDto
    {
      UserId = user.Id.ToString(),
      Email = user.Email ?? string.Empty,
      LastName = profile?.LastName.Value ?? string.Empty,
      FirstName = profile?.FirstName.Value ?? string.Empty,
      AvatarURL = profile?.AvatarUrl?.Value,
      CreateDate = createDateMs
    });
  }
}

