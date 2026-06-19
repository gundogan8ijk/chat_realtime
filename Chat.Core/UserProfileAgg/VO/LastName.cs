using Vogen;

namespace Chat.Core.UserProfileAgg.VO;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public readonly partial struct LastName
{
  private static string NormalizeInput(string value) => value.Trim();

  private static Validation Validate(string value)
      => string.IsNullOrWhiteSpace(value)
          ? Validation.Invalid("LastName cannot be empty.")
          : Validation.Ok;
}

