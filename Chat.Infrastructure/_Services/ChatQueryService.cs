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

    // Batch load user names for active message likes to avoid N+1 query loops
    var activeLikeUserIds = messages
        .SelectMany(x => x.MessageLikes ?? Enumerable.Empty<MessageLike>())
        .Where(l => l.IsActive && l.UserId != myUserId)
        .Select(l => l.UserId.Value)
        .Distinct()
        .ToList();

    var usersLookup = new Dictionary<Guid, string>();
    if (activeLikeUserIds.Count > 0)
    {
      usersLookup = await _dbContext.Users.AsNoTracking()
          .Where(u => activeLikeUserIds.Contains(u.Id))
          .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Người dùng", ct);
    }

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
            usersLookup.TryGetValue(l.UserId.Value, out var name);
            nameLike = name ?? "Người dùng";
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

    var conversationChatIds = rooms.Where(r => r.ConversationChatId != null)
        .Select(r => r.ConversationChatId!.Value)
        .Distinct()
        .ToList();

    var groupChatIds = rooms.Where(r => r.GroupChatId != null)
        .Select(r => GroupChatId.From(r.GroupChatId!.Value))
        .Distinct()
        .ToList();

    // 1. Batch load ConversationChats
    var convsLookup = await _dbContext.ConversationChats.AsNoTracking()
        .Where(c => conversationChatIds.Contains(c.Id))
        .ToDictionaryAsync(c => c.Id, c => c, ct);

    // 2. Batch load GroupChats
    var groupsLookup = await _dbContext.GroupChats.AsNoTracking()
        .Where(g => groupChatIds.Contains(g.Id))
        .ToDictionaryAsync(g => g.Id, g => g, ct);

    // 3. Collect LastSentMessageIds and batch load last messages
    var lastMsgIds = convsLookup.Values.Select(c => c.LastSentMessageId)
        .Concat(groupsLookup.Values.Select(g => g.LastSentMessageId))
        .Where(id => id != null)
        .Select(id => id!.Value)
        .Distinct()
        .ToList();

    var lastMsgsLookup = new Dictionary<MessageId, Message>();
    if (lastMsgIds.Count > 0)
    {
      lastMsgsLookup = await _dbContext.Messages.AsNoTracking()
          .Where(m => lastMsgIds.Contains(m.Id))
          .ToDictionaryAsync(m => m.Id, m => m, ct);
    }

    // 4. Batch load UserConversations
    var userConvs = await _dbContext.UserConversations.AsNoTracking()
        .Where(x => conversationChatIds.Contains(x.ConversationId))
        .ToListAsync(ct);

    var myUserConvsLookup = userConvs.Where(x => x.UserId == myUserId)
        .ToDictionary(x => x.ConversationId, x => x);

    var partnerUserConvsLookup = userConvs.Where(x => x.UserId != myUserId)
        .ToDictionary(x => x.ConversationId, x => x);

    // 5. Batch load UserGroups for group chats
    var myUserGroupsLookup = new Dictionary<GroupChatId, UserGroup>();
    if (groupChatIds.Count > 0)
    {
      myUserGroupsLookup = await _dbContext.UserGroups.AsNoTracking()
          .Where(ug => ug.UserId == myUserId && groupChatIds.Contains(ug.GroupChatId))
          .ToDictionaryAsync(ug => ug.GroupChatId, ug => ug, ct);
    }

    // 6. Fetch names of partner user IDs and sender user IDs in a single query
    var partnerUserIds = userConvs.Where(x => x.UserId != myUserId).Select(x => x.UserId.Value);
    var senderUserIds = lastMsgsLookup.Values.Select(m => m.SenderUserId.Value);
    var allUserIdsToFetch = partnerUserIds.Concat(senderUserIds).Distinct().ToList();

    var usersLookup = new Dictionary<Guid, string>();
    if (allUserIdsToFetch.Count > 0)
    {
      usersLookup = await _dbContext.Users.AsNoTracking()
          .Where(u => allUserIdsToFetch.Contains(u.Id))
          .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "User", ct);
    }

    foreach (var r in rooms)
    {
      if (r.ConversationChatId != null)
      {
        convsLookup.TryGetValue(r.ConversationChatId.Value, out var conv);
        if (conv == null || conv.LastSentMessageId == null) continue;

        lastMsgsLookup.TryGetValue(conv.LastSentMessageId.Value, out var lastMsg);
        if (lastMsg == null) continue;

        myUserConvsLookup.TryGetValue(r.ConversationChatId.Value, out var myUserConv);
        partnerUserConvsLookup.TryGetValue(r.ConversationChatId.Value, out var partnerConv);
        if (partnerConv == null) continue;

        usersLookup.TryGetValue(partnerConv.UserId.Value, out var partnerName);
        partnerName ??= "User";
        string? avatarUrl = null;

        usersLookup.TryGetValue(lastMsg.SenderUserId.Value, out var senderName);
        senderName ??= "User";

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
        var groupKey = GroupChatId.From(r.GroupChatId.Value);
        groupsLookup.TryGetValue(groupKey, out var group);
        if (group == null) continue;

        if (group.LastSentMessageId != null)
        {
          lastMsgsLookup.TryGetValue(group.LastSentMessageId.Value, out var lastMsg);
          if (lastMsg != null)
          {
            usersLookup.TryGetValue(lastMsg.SenderUserId.Value, out var senderName);
            senderName ??= "User";

            myUserGroupsLookup.TryGetValue(group.Id, out var myUserGroup);

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

