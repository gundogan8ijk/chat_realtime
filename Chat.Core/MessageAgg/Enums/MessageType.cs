using Ardalis.SmartEnum;

namespace Chat.Core.MessageAgg.Enums;

public sealed class MessageType(string name, int value, string displayName)
    : SmartEnum<MessageType>(name, value)
{
  public string DisplayName { get; } = displayName;

  public static readonly MessageType Text     = new(nameof(Text),     1, "Văn bản");
  public static readonly MessageType Image    = new(nameof(Image),    2, "Hình ảnh");
  public static readonly MessageType File     = new(nameof(File),     3, "Tệp đính kèm");
  public static readonly MessageType Video    = new(nameof(Video),    4, "Video");
  public static readonly MessageType Voice    = new(nameof(Voice),    5, "Giọng nói");
  public static readonly MessageType Sticker  = new(nameof(Sticker),  6, "Sticker");
  public static readonly MessageType Link     = new(nameof(Link),     7, "Liên kết");
  public static readonly MessageType Location = new(nameof(Location), 8, "Vị trí");
}

