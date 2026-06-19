using Vogen;

namespace Chat.Core.GroupChatAgg.VO;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson)]
public readonly partial struct UserGroupId
{
  private static Guid NormalizeInput(Guid value) => value;
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok
          : Validation.Invalid("UserGroupId cannot be empty.");
}

