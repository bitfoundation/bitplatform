using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IIdentityController : IAppController
{
    [HttpPost]
    Task SignUp(SignUpRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task SendConfirmEmailToken(SendEmailTokenRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ConfirmEmail(ConfirmEmailRequestDto body, CancellationToken cancellationToken);

    [HttpPost]
    Task SendConfirmPhoneToken(SendPhoneTokenRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ConfirmPhone(ConfirmPhoneRequestDto body, CancellationToken cancellationToken);

    [HttpPost]
    Task SendResetPasswordToken(SendResetPasswordTokenRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ResetPassword(ResetPasswordRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<TokenResponseDto> Refresh(RefreshRequestDto body, CancellationToken cancellationToken = default) => default!;

    [HttpPost]
    Task<SignInResponseDto> SignIn(SignInRequestDto body, CancellationToken cancellationToken = default) => default!;

    [HttpPost]
    Task SendTwoFactorToken(IdentityRequestDto body, CancellationToken cancellationToken) => default!;
}
