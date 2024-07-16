using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Resources;

namespace Boilerplate.Shared.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IUserController : IAppController
{
    [HttpGet]
    Task<UserDto> GetCurrentUser(CancellationToken cancellationToken = default);

    [HttpGet]
    Task<List<UserSessionDto>> GetUserSessions(CancellationToken cancellationToken);

    [HttpPost]
    Task SignOut(CancellationToken cancellationToken);

    [HttpPost("{sessionIdToBeRemoved}")]
    Task RevokeSession(Guid sessionIdToBeRemoved, CancellationToken cancellationToken);

    [HttpPut]
    Task<UserDto> Update(EditUserDto userDto, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangeUserName(ChangeUserNameRequestDto request, CancellationToken cancellationToken = default);

    [HttpPost]
    Task SendChangeEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken = default);

    [HttpPost]
    Task SendChangePhoneNumberToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangePhoneNumber(ChangePhoneNumberRequestDto request, CancellationToken cancellationToken = default);

    [HttpDelete]
    Task Delete(CancellationToken cancellationToken = default);

    [HttpPost]
    [Route("~/api/[controller]/2fa")]
    Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken = default) => default!;
}
