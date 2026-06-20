using Chat.Core.AccountAgg;
using Chat.Core._ValueObjects;
using Chat.Core.UserProfileAgg;
using Chat.Core.UserProfileAgg.VO;
using Microsoft.AspNetCore.Identity;
using Ardalis.SharedKernel;
using Chat.UseCases.ChatApp;

namespace Chat.UseCases.ChatApp.Commands;

public record SignInCommand(string Email, string Password, string DeviceId)
    : ICommand<Result<SignInResponseDto>>;

public record SignInResponseDto(TemplateTokenDto AccessToken, TemplateTokenDto RefreshToken);

public class SignInCommandHandler(
    UserManager<ApplicationUser> userManager,
    IReadRepository<UserProfile> userProfileRepository,
    ITokenService tokenService)
  : ICommandHandler<SignInCommand, Result<SignInResponseDto>>
{
  private readonly UserManager<ApplicationUser> _userManager = userManager;
  private readonly IReadRepository<UserProfile> _userProfileRepository = userProfileRepository;
  private readonly ITokenService _tokenService = tokenService;

  public async ValueTask<Result<SignInResponseDto>> Handle(SignInCommand request, CancellationToken cancellationToken)
  {
    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
    {
      return Result<SignInResponseDto>.Error("Tài khoản hoặc mật khẩu không chính xác");
    }

    var userId = UserId.From(user.Id);
    var profile = await _userProfileRepository.GetByIdAsync(userId, cancellationToken);
    if (profile == null)
    {
      return Result<SignInResponseDto>.Error("Không tìm thấy thông tin profile của người dùng");
    }

    var tokens = await _tokenService.GenerateTokensAsync(user, profile, DeviceId.From(request.DeviceId));
    if (tokens.AccessToken == null || tokens.RefreshToken == null)
    {
      return Result<SignInResponseDto>.Error("Tạo token thất bại");
    }

    return Result.Success(new SignInResponseDto(tokens.AccessToken, tokens.RefreshToken));
  }
}
