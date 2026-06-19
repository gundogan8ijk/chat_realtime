using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Queries;

public record GetUserByEmailQuery(string Email, string MyEmail, Guid MyId) : IQuery<Result<UserSearchDto>>;

public class GetUserByEmailQueryHandler(IChatQueryService chatQueryService)
  : IQueryHandler<GetUserByEmailQuery, Result<UserSearchDto>>
{
  private readonly IChatQueryService _chatQueryService = chatQueryService;

  public async ValueTask<Result<UserSearchDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
  {
    var res = await _chatQueryService.SearchUserByEmailAsync(request.Email, request.MyEmail, request.MyId, cancellationToken);
    if (res == null)
    {
      return Result<UserSearchDto>.NotFound("Không tìm thấy người dùng có email này hoặc mối quan hệ bị giới hạn.");
    }
    return Result.Success(res);
  }
}

