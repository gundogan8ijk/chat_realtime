using Chat.UseCases.ChatApp;
using Ardalis.SharedKernel;
using Chat.Core._ValueObjects;
using Chat.Core.ConversationChatAgg;
using Chat.Core.ConversationChatAgg.VO;

namespace Chat.UseCases.ChatApp.Commands;

public record GetOrCreatConversationCommand(Guid MyId, Guid PartnerId) : ICommand<Result<string>>;

public class GetOrCreatConversationCommandHandler(
    IChatQueryService chatQueryService,
    IRepository<ConversationChat> convRepository,
    IRepository<RoomMessage> roomMsgRepository)
  : ICommandHandler<GetOrCreatConversationCommand, Result<string>>
{
  private readonly IChatQueryService _chatQueryService = chatQueryService;
  private readonly IRepository<ConversationChat> _convRepository = convRepository;
  private readonly IRepository<RoomMessage> _roomMsgRepository = roomMsgRepository;

  public async ValueTask<Result<string>> Handle(GetOrCreatConversationCommand request, CancellationToken cancellationToken)
  {
    string roomId = await _chatQueryService.GetIdConversationChatAsync(request.MyId, request.PartnerId, cancellationToken) ?? string.Empty;

    if (string.IsNullOrEmpty(roomId))
    {
      var myUserId = UserId.From(request.MyId);
      var partnerUserId = UserId.From(request.PartnerId);

      var converChat = ConversationChat.Create();
      converChat.AddParticipant(myUserId, partnerUserId);
      converChat.AddParticipant(partnerUserId, myUserId);

      // Save ConversationChat which also saves UserConversations via EF relationship mapping
      await _convRepository.AddAsync(converChat, cancellationToken);

      // Get participants to create RoomMessages
      var user1 = converChat.UserConversations.First(x => x.UserId == myUserId);
      var user2 = converChat.UserConversations.First(x => x.UserId == partnerUserId);

      var roomMsg1 = RoomMessage.CreateForConversation(myUserId, converChat.Id, user1.Id);
      var roomMsg2 = RoomMessage.CreateForGroup(partnerUserId, converChat.Id.Value, user2.Id);

      await _roomMsgRepository.AddAsync(roomMsg1, cancellationToken);
      await _roomMsgRepository.AddAsync(roomMsg2, cancellationToken);

      roomId = converChat.Id.Value.ToString();
    }

    return Result<string>.Success(roomId);
  }
}

