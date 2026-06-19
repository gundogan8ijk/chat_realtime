using Vogen;

namespace Chat.Core._ValueObjects;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public readonly partial struct AvatarUrl
{
  private static string NormalizeInput(string value) => value.Trim();

  private static Validation Validate(string value)
      => string.IsNullOrWhiteSpace(value)
          ? Validation.Invalid("AvatarUrl cannot be empty.")
          : Validation.Ok;
}

