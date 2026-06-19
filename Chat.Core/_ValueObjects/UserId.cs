using Vogen;

namespace Chat.Core._ValueObjects;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson)]
public readonly partial struct UserId
{
  private static Validation Validate(Guid value)
      => value == Guid.Empty
          ? Validation.Invalid("UserId cannot be empty")
          : Validation.Ok;
}

