using Ardalis.SmartEnum;

namespace Chat.Core.FriendshipAgg.Enums;

public sealed class FriendStatus(string name, int value, string displayName)
    : SmartEnum<FriendStatus>(name, value)
{
  public string DisplayName { get; } = displayName;

  public static readonly FriendStatus APending = new(nameof(APending), 1, "Đang chờ A");
  public static readonly FriendStatus Connect  = new(nameof(Connect),  2, "Kết bạn");
  public static readonly FriendStatus BPending = new(nameof(BPending), 3, "Đang chờ B");
  public static readonly FriendStatus ABlocked = new(nameof(ABlocked), 4, "A chặn");
  public static readonly FriendStatus BBlocked = new(nameof(BBlocked), 5, "B chặn");
  public static readonly FriendStatus NotGiven = new(nameof(NotGiven), 6, "Chưa xác định");
}

