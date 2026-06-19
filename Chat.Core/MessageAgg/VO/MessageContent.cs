using Vogen;

namespace Chat.Core.MessageAgg.VO;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public readonly partial struct MessageContent
{
  private static string NormalizeInput(string value) => value.Trim();

  private static Validation Validate(string value)
      => string.IsNullOrWhiteSpace(value)
          ? Validation.Invalid("MessageContent cannot be empty.")
          : value.Length > 1000
              ? Validation.Invalid("MessageContent is too long (max 1000 chars).")
              : Validation.Ok;
}

