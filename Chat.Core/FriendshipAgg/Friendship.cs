using Chat.Core._ValueObjects;
using Chat.Core.FriendshipAgg.VO;
using Chat.Core.FriendshipAgg.Enums;

namespace Chat.Core.FriendshipAgg;

public class Friendship : EntityBase<Friendship, FriendshipId>, IAggregateRoot
{
  public UserId       UserId_A  { get; private set; }
  public UserId       UserId_B  { get; private set; }
  public FriendStatus Status    { get; private set; } = FriendStatus.APending;
  public DateTime     CreatedAt { get; private set; } = DateTime.UtcNow;

  private Friendship() { }

  public static Friendship Create(UserId userIdA, UserId userIdB)
      => new()
      {
        Id        = FriendshipId.From(Guid.NewGuid()),
        UserId_A  = userIdA,
        UserId_B  = userIdB,
        Status    = FriendStatus.APending,
        CreatedAt = DateTime.UtcNow
      };

  public void Accept()   => Status = FriendStatus.Connect;
  public void BlockByA() => Status = FriendStatus.ABlocked;
  public void BlockByB() => Status = FriendStatus.BBlocked;
}

