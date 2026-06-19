using Chat.Core._ValueObjects;
using Chat.Core.RefreshTokenAgg.VO;

namespace Chat.Core.RefreshTokenAgg;

public class RefreshToken : EntityBase<RefreshToken, Guid>, IAggregateRoot
{
  public string JwtId { get; private set; } = string.Empty;
  public DeviceId DeviceId { get; private set; }
  public UserId UserId { get; private set; }
  public string Token { get; private set; } = string.Empty;
  public bool IsUsed { get; private set; }
  public bool IsRevoked { get; private set; }
  public DateTime IssuedAt { get; private set; } = DateTime.UtcNow;
  public DateTime ExpireAt { get; private set; }

  private RefreshToken() { }

  public static RefreshToken Create(UserId userId, string token, string jwtId, DeviceId deviceId, DateTime expireAt)
  {
    return new RefreshToken
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Token = token,
      JwtId = jwtId,
      DeviceId = deviceId,
      ExpireAt = expireAt,
      IssuedAt = DateTime.UtcNow,
      IsUsed = false,
      IsRevoked = false
    };
  }

  public void Revoke() => IsRevoked = true;
  public void Use() => IsUsed = true;
}

