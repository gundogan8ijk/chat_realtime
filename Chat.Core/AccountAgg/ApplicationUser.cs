using Microsoft.AspNetCore.Identity;

namespace Chat.Core.AccountAgg;

public class ApplicationUser : IdentityUser<Guid>
{
  public DateTime LastModified { get; set; }
  public DateTime ActivatedAt { get; set; }
  public string? ReasonLock { get; set; }
  public int TypeBanned { get; set; }
  public DateTime? TimeBanned { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
  public int ValueRole { get; set; }
}

