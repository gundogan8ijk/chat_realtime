using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Queries;

public record GetListItemChatActiveQuery(Guid MyId, int PageSize, int PageNumber) : IQuery<Result<List<ItemChatDto>>>;

public class GetListItemChatActiveQueryHandler(IChatQueryService chatQueryService)
  : IQueryHandler<GetListItemChatActiveQuery, Result<List<ItemChatDto>>>
{
  private readonly IChatQueryService _chatQueryService = chatQueryService;

  public async ValueTask<Result<List<ItemChatDto>>> Handle(GetListItemChatActiveQuery request, CancellationToken cancellationToken)
  {
    var list = await _chatQueryService.GetListItemChatActiveAsync(request.MyId, request.PageSize, request.PageNumber, cancellationToken);
    return Result.Success(list);
  }
}

