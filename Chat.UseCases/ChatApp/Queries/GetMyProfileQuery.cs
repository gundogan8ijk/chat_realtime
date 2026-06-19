using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Queries;

public record GetMyProfileQuery(Guid MyId) : IQuery<Result<UserDto>>;

public class GetMyProfileQueryHandler(IChatQueryService chatQueryService)
    : IQueryHandler<GetMyProfileQuery, Result<UserDto>>
{
  private readonly IChatQueryService _chatQueryService = chatQueryService;

  public async ValueTask<Result<UserDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
  {
    return await _chatQueryService.GetMyProfileUserAsync(request.MyId, cancellationToken);
  }
}

