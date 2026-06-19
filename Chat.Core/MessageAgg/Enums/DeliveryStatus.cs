using Ardalis.SmartEnum;

namespace Chat.Core.MessageAgg.Enums;

public sealed class DeliveryStatus(string name, int value, string displayName)
    : SmartEnum<DeliveryStatus>(name, value)
{
  public string DisplayName { get; } = displayName;

  public static readonly DeliveryStatus Sending   = new(nameof(Sending),   0, "Đang gửi");
  public static readonly DeliveryStatus Sent      = new(nameof(Sent),      1, "Đã gửi");
  public static readonly DeliveryStatus Read      = new(nameof(Read),      2, "Đã đọc");
  public static readonly DeliveryStatus DeletedAt = new(nameof(DeletedAt), 3, "Đã thu hồi");
  public static readonly DeliveryStatus IsDeleted = new(nameof(IsDeleted), 4, "Đã xoá");
}

