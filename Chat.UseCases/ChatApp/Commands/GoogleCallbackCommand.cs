using Chat.Core.AccountAgg;
using Chat.Core._ValueObjects;
using Chat.Core.UserProfileAgg;
using Chat.Core.UserProfileAgg.VO;
using Microsoft.AspNetCore.Identity;
using Ardalis.SharedKernel;
using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Commands;

public record GoogleCallbackCommand(
    string Email,
    string GoogleId,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string DeviceId)
  : ICommand<Result<GoogleCallbackResponseDto>>;

public record GoogleCallbackResponseDto(TemplateTokenDto AccessToken, TemplateTokenDto RefreshToken);

public class GoogleCallbackCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IRepository<UserProfile> userProfileRepository,
    ITokenService tokenService,
    IMediator mediator)
  : ICommandHandler<GoogleCallbackCommand, Result<GoogleCallbackResponseDto>>
{
  private readonly UserManager<ApplicationUser> _userManager = userManager;
  private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
  private readonly IRepository<UserProfile> _userProfileRepository = userProfileRepository;
  private readonly ITokenService _tokenService = tokenService;
  private readonly IMediator _mediator = mediator;

  public async ValueTask<Result<GoogleCallbackResponseDto>> Handle(GoogleCallbackCommand request, CancellationToken cancellationToken)
  {
    var info = new UserLoginInfo("Google", request.GoogleId, "Google");
    var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

    if (user == null)
    {
      user = await _userManager.FindByEmailAsync(request.Email);

      if (user == null)
      {
        user = new ApplicationUser
        {
          UserName = request.Email,
          Email = request.Email,
          EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
          var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));
          return Result<GoogleCallbackResponseDto>.Error($"Không thể tạo tài khoản từ Google: {errors}");
        }

        const string defaultRole = "User";
        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
          await _roleManager.CreateAsync(new ApplicationRole { Name = defaultRole, ValueRole = 1 });
        }
        await _userManager.AddToRoleAsync(user, defaultRole);

        // Save profile to database
        var userId = UserId.From(user.Id);
        var firstNameVal = FirstName.From(request.FirstName);
        var lastNameVal = LastName.From(request.LastName);
        var avatarUrlVal = string.IsNullOrEmpty(request.AvatarUrl) ? (AvatarUrl?)null : AvatarUrl.From(request.AvatarUrl);

        var profile = UserProfile.Create(userId, firstNameVal, lastNameVal, avatarUrlVal);
        await _userProfileRepository.AddAsync(profile, cancellationToken);

        // Save profile to Redis
        var redisCmd = new AddProfileUserRedisCommand(userId, firstNameVal, lastNameVal, request.Email, avatarUrlVal);
        await _mediator.Send(redisCmd, cancellationToken);
      }

      var linkResult = await _userManager.AddLoginAsync(user, info);
      if (!linkResult.Succeeded)
      {
        var errors = string.Join(", ", linkResult.Errors.Select(x => x.Description));
        return Result<GoogleCallbackResponseDto>.Error($"Không thể liên kết tài khoản Google: {errors}");
      }
    }

    var dbProfile = await _userProfileRepository.GetByIdAsync(UserId.From(user.Id), cancellationToken);

    if (dbProfile == null)
    {
      var userId = UserId.From(user.Id);
      var firstNameVal = FirstName.From(request.FirstName);
      var lastNameVal = LastName.From(request.LastName);
      var avatarUrlVal = string.IsNullOrEmpty(request.AvatarUrl) ? (AvatarUrl?)null : AvatarUrl.From(request.AvatarUrl);

      dbProfile = UserProfile.Create(userId, firstNameVal, lastNameVal, avatarUrlVal);
      await _userProfileRepository.AddAsync(dbProfile, cancellationToken);

      var redisCmd = new AddProfileUserRedisCommand(userId, firstNameVal, lastNameVal, request.Email, avatarUrlVal);
      await _mediator.Send(redisCmd, cancellationToken);
    }

    var tokens = await _tokenService.GenerateTokensAsync(user, dbProfile, DeviceId.From(request.DeviceId));
    if (tokens.AccessToken == null || tokens.RefreshToken == null)
    {
      return Result<GoogleCallbackResponseDto>.Error("Tạo token thất bại");
    }

    return Result.Success(new GoogleCallbackResponseDto(tokens.AccessToken, tokens.RefreshToken));
  }
}
