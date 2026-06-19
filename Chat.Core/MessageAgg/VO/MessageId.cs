using Vogen;

namespace Chat.Core.MessageAgg.VO;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson)]
public readonly partial struct MessageId
{
  private static Guid NormalizeInput(Guid value) => value;
  private static Validation Validate(Guid value)
      => value != Guid.Empty
          ? Validation.Ok
          : Validation.Invalid($"{nameof(MessageId)} cannot be empty.");
}

