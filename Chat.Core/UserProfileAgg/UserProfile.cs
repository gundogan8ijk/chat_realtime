using Chat.Core._ValueObjects;
using Chat.Core.UserProfileAgg.VO;

namespace Chat.Core.UserProfileAgg;

public class UserProfile : EntityBase<UserProfile, UserId>, IAggregateRoot
{
  public FirstName FirstName { get; private set; }
  public LastName LastName { get; private set; }
  public AvatarUrl? AvatarUrl { get; private set; }
  public DateTime CreateDate { get; private set; } = DateTime.UtcNow;

  private UserProfile() { }

  public static UserProfile Create(UserId id, FirstName firstName, LastName lastName, AvatarUrl? avatarUrl)
  {
    return new UserProfile
    {
      Id = id,
      FirstName = firstName,
      LastName = lastName,
      AvatarUrl = avatarUrl,
      CreateDate = DateTime.UtcNow
    };
  }

  public void UpdateProfile(FirstName firstName, LastName lastName, AvatarUrl? avatarUrl)
  {
    FirstName = firstName;
    LastName = lastName;
    AvatarUrl = avatarUrl;
  }
}

