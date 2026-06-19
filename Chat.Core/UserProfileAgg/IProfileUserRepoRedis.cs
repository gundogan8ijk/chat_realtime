namespace Chat.Core.UserProfileAgg;

public interface IProfileUserRepoRedis
{
  Task<bool> AddProfileUserRedisAsync(UserProfile profile, string email, CancellationToken ct = default);
}

