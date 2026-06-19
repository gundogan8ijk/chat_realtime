using Vogen;

namespace Chat.Core.FriendshipAgg.VO;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson)]
public readonly partial struct FriendshipId
{
  private static Guid NormalizeInput(Guid value) => value;
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok
          : Validation.Invalid("FriendshipId cannot be empty.");
}

