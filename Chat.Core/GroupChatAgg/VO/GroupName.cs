using Vogen;

namespace Chat.Core.GroupChatAgg.VO;

[ValueObject<string>(conversions: Conversions.SystemTextJson)]
public readonly partial struct GroupName
{
  private static string NormalizeInput(string value) => value.Trim();

  private static Validation Validate(string value)
      => string.IsNullOrWhiteSpace(value)
          ? Validation.Invalid("GroupName cannot be empty.")
          : value.Length > 200
              ? Validation.Invalid("GroupName is too long (max 200 chars).")
              : Validation.Ok;
}

