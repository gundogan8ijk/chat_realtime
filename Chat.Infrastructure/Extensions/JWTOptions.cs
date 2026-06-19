using System.ComponentModel.DataAnnotations;

namespace Chat.Infrastructure.Extensions;

public class JWTOptions
{
  public const string JWTOptionsKey = "JWTOptions";
  [Required]
  public string Secret { get; set; } = string.Empty;
  [Required]
  public string Issuer { get; set; } = string.Empty;
  [Required]
  public string Audience { get; set; } = string.Empty;
  public int ExpirationTimeInMinutes { get; set; }
}

