namespace Chat.Core.AccountAgg;

public class TemplateTokenDto
{
  public string ContentToken { get; set; } = string.Empty;
  public DateTime ExpiresAt { get; set; }
}

public class TokenDto
{
  public TemplateTokenDto? AccessToken { get; set; }
  public TemplateTokenDto? RefreshToken { get; set; }
}

public class ClaimsAccessTokenDto
{
  public string UserId { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public List<string> Roles { get; set; } = new();

  public ClaimsAccessTokenDto() { }

  public ClaimsAccessTokenDto(string userId, string email, string lastName, string firstName, List<string> roles)
  {
    UserId = userId;
    Email = email;
    LastName = lastName;
    FirstName = firstName;
    Roles = roles;
  }
}

