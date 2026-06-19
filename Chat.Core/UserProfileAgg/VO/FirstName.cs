using Vogen;

namespace Chat.Core.UserProfileAgg.VO;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public readonly partial struct FirstName
{
  private static string NormalizeInput(string value) => value.Trim();

  private static Validation Validate(string value)
      => string.IsNullOrWhiteSpace(value)
          ? Validation.Invalid("FirstName cannot be empty.")
          : Validation.Ok;
}

