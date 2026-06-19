using Ardalis.SmartEnum;

namespace Chat.Core.MessageAgg.Enums;

public sealed class LikeType(string name, int value, string displayName)
    : SmartEnum<LikeType>(name, value)
{
  public string DisplayName { get; } = displayName;

  public static readonly LikeType Like    = new(nameof(Like),    1, "Thích");
  public static readonly LikeType Love    = new(nameof(Love),    2, "Yêu thích");
  public static readonly LikeType Haha    = new(nameof(Haha),    3, "Haha");
  public static readonly LikeType Wow     = new(nameof(Wow),     4, "Wow");
  public static readonly LikeType Sad     = new(nameof(Sad),     5, "Buồn");
  public static readonly LikeType Angry   = new(nameof(Angry),   6, "Tức giận");
  public static readonly LikeType UnLike  = new(nameof(UnLike),  7, "Bỏ thích");
  public static readonly LikeType NotLike = new(nameof(NotLike), 8, "Không thích");
}

