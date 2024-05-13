using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Resources;

namespace Boilerplate.Client.Core.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IUserController : IAppController
{
    [HttpGet]
    Task<UserDto> GetCurrentUser(CancellationToken cancellationToken = default);

    [HttpPut]
    Task<UserDto> Update(EditUserDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangePassword(ChangePasswordRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangeUserName(ChangeUserNameRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task SendChangeEmailToken(SendEmailTokenRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangeEmail(ChangeEmailRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task SendChangePhoneNumberToken(SendPhoneNumberTokenRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ChangePhoneNumber(ChangePhoneNumberRequestDto body, CancellationToken cancellationToken = default);

    [HttpDelete]
    Task Delete(CancellationToken cancellationToken = default);

    [HttpPost]
    [Route("~/api/[controller]/manage/2fa")]
    Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto twoFactorAuthRequest, CancellationToken cancellationToken = default) => default!;
}
