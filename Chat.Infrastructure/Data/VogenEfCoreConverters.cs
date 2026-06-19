

using Vogen;

namespace Chat.Infrastructure.Data;

[EfCoreConverter<MessageId>]
[EfCoreConverter<ConversationId>]
[EfCoreConverter<RoomId>]
[EfCoreConverter<UserId>]
[EfCoreConverter<MessageContent>]
[EfCoreConverter<GroupName>]
[EfCoreConverter<AvatarUrl>]
[EfCoreConverter<FriendshipId>]
[EfCoreConverter<GroupChatId>]
[EfCoreConverter<UserGroupId>]
internal partial class VogenEfCoreConverters;

