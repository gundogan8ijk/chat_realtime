namespace Chat.UseCases.ChatApp;

public class TitleRoomDialoDto
{
  public TitleRoomDialoDto()
  {
  }

  public string PartnersId { get; set; } = string.Empty;
  public string NamePartners { get; set; } = string.Empty;
  public bool IsFriend { get; set; } = false;
  public string? AvatarUrl { get; set; }
  public string RoomChatId { get; set; } = string.Empty;
  public string? LastReadMessId { get; set; }
  public bool IsGroup { get; set; } = false;

  public TitleRoomDialoDto(string partnersId, string namePartners, bool isFriend, string? avatarUrl, string roomChatId, Guid? lastReadMessId)
  {
    PartnersId = partnersId;
    NamePartners = namePartners;
    IsFriend = isFriend;
    AvatarUrl = avatarUrl;
    RoomChatId = roomChatId;
    LastReadMessId = lastReadMessId?.ToString();
  }
}

public class UserLikeMessDto
{
  public UserLikeMessDto()
  {
  }

  public string NameUserLike { get; set; } = null!;
  public string LikeTypeStr { get; set; } = null!;

  public UserLikeMessDto(string nameUserLike, string likeTypeStr)
  {
    NameUserLike = nameUserLike;
    LikeTypeStr = likeTypeStr;
  }
}

public class ItemMessDialogueDto
{
  public string MessageId { get; set; } = null!;
  public bool IsMeSend { get; set; } = true;
  public string? MessageContent { get; set; }
  public string MessageType { get; set; } = null!;
  public string CreateDate { get; set; } = null!;
  public string? ParentMessageId { get; set; }
  public string? ParentMessageContent { get; set; }
  public List<UserLikeMessDto>? UserLikeMess { get; set; }

  public ItemMessDialogueDto()
  {
  }

  public ItemMessDialogueDto(Guid messageId, bool isMeSend, string? messageContent,
                         string messageType, DateTime createDate, List<UserLikeMessDto>? userLikeMess)
  {
    MessageId = messageId.ToString();
    IsMeSend = isMeSend;
    MessageContent = messageContent;
    MessageType = messageType;
    CreateDate = ((DateTimeOffset)createDate).ToUnixTimeMilliseconds().ToString();
    UserLikeMess = userLikeMess;
  }

  public ItemMessDialogueDto(Guid messageId, bool isMeSend, string? messageContent,
                         string messageType, DateTime createDate, List<UserLikeMessDto>? userLikeMess,
                         Guid? parentMessageId, string? parentMessageContent)
      : this(messageId, isMeSend, messageContent, messageType, createDate, userLikeMess)
  {
    ParentMessageId = parentMessageId?.ToString();
    ParentMessageContent = parentMessageContent;
  }
}

public class FrameDialoChatDto
{
  public FrameDialoChatDto()
  {
  }

  public TitleRoomDialoDto? TitleRoom { get; set; }
  public List<ItemMessDialogueDto>? ListMess { get; set; }

  public FrameDialoChatDto(TitleRoomDialoDto? titleRoom, List<ItemMessDialogueDto>? listMess)
  {
    TitleRoom = titleRoom;
    ListMess = listMess;
  }
}

public class ItemChatDto
{
  public string RoomId { get; set; } = null!;
  public string DisplayName { get; set; } = null!;
  public string? AvatarUrl { get; set; }
  public string? NameSender { get; set; }
  public string? TextType { get; set; }
  public bool IsGroup { get; set; } = false;
  public bool IsRead { get; set; } = false;
  public string? LastSentMessageContent { get; set; }
  public string? TimeSentMessLast { get; set; }

  public ItemChatDto()
  {
  }

  public ItemChatDto(string roomId, string displayName, string? avatarUrl, string? lastSentMessage, string? nameSender, DateTime timeLastMess, string? textType, bool isRead)
  {
    RoomId = roomId;
    DisplayName = displayName;
    AvatarUrl = avatarUrl;
    LastSentMessageContent = lastSentMessage;
    NameSender = nameSender;
    TimeSentMessLast = ((DateTimeOffset)timeLastMess).ToUnixTimeMilliseconds().ToString();
    TextType = textType;
    IsRead = isRead;
  }

  public ItemChatDto(string roomId, string displayName, string? avatarUrl, bool isGroup, string? lastSentMessage, string? nameSender, DateTime timeLastMess, string? textType, bool isRead)
  {
    RoomId = roomId;
    DisplayName = displayName;
    AvatarUrl = avatarUrl;
    IsGroup = isGroup;
    LastSentMessageContent = lastSentMessage;
    NameSender = nameSender;
    TimeSentMessLast = ((DateTimeOffset)timeLastMess).ToUnixTimeMilliseconds().ToString();
    TextType = textType;
    IsRead = isRead;
  }

  public ItemChatDto(string roomId, string displayName, string? avatarUrl)
  {
    RoomId = roomId;
    DisplayName = displayName;
    AvatarUrl = avatarUrl;
    IsGroup = true;
  }
}

public class UserDto
{
  public string UserId { get; set; } = string.Empty;
  public string? AvatarURL { get; set; }
  public string? LastName { get; set; }
  public string? FirstName { get; set; }
  public string Email { get; set; } = null!;
  public string? CreateDate { get; set; }

  public UserDto()
  {
  }

  public UserDto(string userId, string? avatarURL, string? lastName, string? firstName, string email, DateTime createDate)
  {
    UserId = userId;
    AvatarURL = avatarURL;
    LastName = lastName;
    FirstName = firstName;
    Email = email;
    CreateDate = ((DateTimeOffset)createDate).ToUnixTimeMilliseconds().ToString();
  }
}

public class UserSearchDto
{
  public UserDto UserDto { get; set; } = null!;
  public string? FriendStatus { get; set; }

  public UserSearchDto()
  {
  }

  public UserSearchDto(UserDto userDto, string? friendStatus = null)
  {
    UserDto = userDto;
    FriendStatus = friendStatus;
  }
}

