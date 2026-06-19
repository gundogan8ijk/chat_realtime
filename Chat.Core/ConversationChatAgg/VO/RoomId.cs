using Vogen;

namespace Chat.Core.ConversationChatAgg.VO;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson)]
public readonly partial struct RoomId
{
  private static Guid NormalizeInput(Guid value) => value;
  private static Validation Validate(Guid value)
      => value != Guid.Empty
          ? Validation.Ok
          : Validation.Invalid($"{nameof(RoomId)} cannot be empty.");
}

