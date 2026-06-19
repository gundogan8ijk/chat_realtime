namespace Chat.UseCases.ChatApp.Commands;

public record AddProfileUserRedisCommand(UserId UserId, FirstName FirstName, LastName LastName, string Email, AvatarUrl? AvatarUrl)
    : ICommand<Result<bool>>;

public class AddProfileUserRedisCommandHandler(IProfileUserRepoRedis repo)
    : ICommandHandler<AddProfileUserRedisCommand, Result<bool>>
  {
    private readonly IProfileUserRepoRedis _repo = repo;

    public async ValueTask<Result<bool>> Handle(AddProfileUserRedisCommand request, CancellationToken cancellationToken)
    {
      var profile = UserProfile.Create(request.UserId, request.FirstName, request.LastName, request.AvatarUrl);
      var success = await _repo.AddProfileUserRedisAsync(profile, request.Email, cancellationToken);
      if (!success)
      {
        return Result.Error("Ghi profile vào Redis thất bại");
      }
      return Result.Success(true);
    }
  }

