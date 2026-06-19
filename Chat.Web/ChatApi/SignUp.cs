using Chat.Core.AccountAgg;

using Chat.Infrastructure.Data.Context;
using Chat.UseCases.ChatApp.Commands;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class SignUp(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ChatDbContext dbContext,
    IMediator mediator)
  : Endpoint<SignUpRequest, Results<Ok<SignUpResponse>, BadRequest<string>>>
{
  public override void Configure()
  {
    Post("/api/AccountNormal/SignUp");
    AllowAnonymous();
  }

  public override async Task<Results<Ok<SignUpResponse>, BadRequest<string>>> ExecuteAsync(SignUpRequest req, CancellationToken ct)
  {
    var user = new ApplicationUser
    {
      UserName = req.Email,
      Email = req.Email
    };

    var result = await userManager.CreateAsync(user, req.Password);
    if (!result.Succeeded)
    {
      var errorMsg = string.Join("\n", result.Errors.Select(e => e.Description));
      return TypedResults.BadRequest(errorMsg);
    }

    // Role check and create
    const string defaultRole = "User";
    if (!await roleManager.RoleExistsAsync(defaultRole))
    {
      await roleManager.CreateAsync(new ApplicationRole { Name = defaultRole, ValueRole = 1 });
    }
    await userManager.AddToRoleAsync(user, defaultRole);

    // Save profile to database
    var userId = UserId.From(user.Id);
    var firstNameVal = FirstName.From(req.FirstName);
    var lastNameVal = LastName.From(req.LastName);
    
    var profile = UserProfile.Create(userId, firstNameVal, lastNameVal, null);
    await dbContext.UserProfiles.AddAsync(profile, ct);
    await dbContext.SaveChangesAsync(ct);

    // Save profile to Redis using command
    var redisCmd = new AddProfileUserRedisCommand(userId, firstNameVal, lastNameVal, req.Email, null);
    await mediator.Send(redisCmd, ct);

    return TypedResults.Ok(new SignUpResponse
    {
      Succeeded = true,
      Email = req.Email,
      LastName = req.LastName,
      FirstName = req.FirstName
    });
  }
}

public class SignUpRequest
{
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
}

public class SignUpResponse
{
  public bool Succeeded { get; set; }
  public string Email { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
}

