using Chat.Core._ValueObjects;
using Chat.Core.UserProfileAgg;
using Chat.Core.RefreshTokenAgg.VO;

namespace Chat.Core.AccountAgg;

public interface ITokenService
{
  Task<TokenDto> GenerateTokensAsync(ApplicationUser user, UserProfile profile, DeviceId deviceId);
  Task<Result<TemplateTokenDto>> RenewAccessTokenAsync(string refreshToken, DeviceId deviceId);
}

