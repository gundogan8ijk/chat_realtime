using Vogen;

namespace Chat.Core.ConversationChatAgg.VO;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson)]
public readonly partial struct ConversationId
{
  private static Guid NormalizeInput(Guid value) => value;
  private static Validation Validate(Guid value)
      => value != Guid.Empty
          ? Validation.Ok
          : Validation.Invalid($"{nameof(ConversationId)} cannot be empty.");
}

