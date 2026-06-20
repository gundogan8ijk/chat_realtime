using Chat.Core.AccountAgg;
using Chat.Core._ValueObjects;
using Chat.Core.UserProfileAgg;
using Chat.Core.UserProfileAgg.VO;
using Microsoft.AspNetCore.Identity;
using Ardalis.SharedKernel;
using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Commands;

public record SignUpCommand(string Email, string Password, string FirstName, string LastName)
    : ICommand<Result<SignUpResponseDto>>;

public record SignUpResponseDto(bool Succeeded, string Email, string LastName, string FirstName);

public class SignUpCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IRepository<UserProfile> userProfileRepository,
    IMediator mediator)
  : ICommandHandler<SignUpCommand, Result<SignUpResponseDto>>
{
  private readonly UserManager<ApplicationUser> _userManager = userManager;
  private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
  private readonly IRepository<UserProfile> _userProfileRepository = userProfileRepository;
  private readonly IMediator _mediator = mediator;

  public async ValueTask<Result<SignUpResponseDto>> Handle(SignUpCommand request, CancellationToken cancellationToken)
  {
    var user = new ApplicationUser
    {
      UserName = request.Email,
      Email = request.Email
    };

    var result = await _userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
    {
      var errorMsg = string.Join("\n", result.Errors.Select(e => e.Description));
      return Result<SignUpResponseDto>.Error(errorMsg);
    }

    const string defaultRole = "User";
    if (!await _roleManager.RoleExistsAsync(defaultRole))
    {
      await _roleManager.CreateAsync(new ApplicationRole { Name = defaultRole, ValueRole = 1 });
    }
    await _userManager.AddToRoleAsync(user, defaultRole);

    var userId = UserId.From(user.Id);
    var firstNameVal = FirstName.From(request.FirstName);
    var lastNameVal = LastName.From(request.LastName);

    var profile = UserProfile.Create(userId, firstNameVal, lastNameVal, null);
    await _userProfileRepository.AddAsync(profile, cancellationToken);

    // Save profile to Redis
    var redisCmd = new AddProfileUserRedisCommand(userId, firstNameVal, lastNameVal, request.Email, null);
    await _mediator.Send(redisCmd, cancellationToken);

    return Result.Success(new SignUpResponseDto(true, request.Email, request.LastName, request.FirstName));
  }
}
