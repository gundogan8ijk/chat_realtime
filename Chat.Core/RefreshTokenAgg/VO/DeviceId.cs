using Vogen;

namespace Chat.Core.RefreshTokenAgg.VO;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public readonly partial struct DeviceId
{
  private static string NormalizeInput(string value) => value.Trim();

  private static Validation Validate(string value)
      => string.IsNullOrWhiteSpace(value)
          ? Validation.Invalid("DeviceId cannot be empty.")
          : Validation.Ok;
}

