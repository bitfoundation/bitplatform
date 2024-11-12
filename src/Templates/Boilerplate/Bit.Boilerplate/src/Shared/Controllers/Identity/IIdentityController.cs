//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Shared.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IIdentityController : IAppController
{
    [HttpPost]
    Task SendConfirmEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task<SignInResponseDto> ConfirmEmail(ConfirmEmailRequestDto request, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task SendConfirmPhoneToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task<SignInResponseDto> ConfirmPhone(ConfirmPhoneRequestDto request, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task SendResetPasswordToken(SendResetPasswordTokenRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken);

    public const string RefreshUri = "api/Identity/Refresh";
    [HttpPost]
    Task<TokenResponseDto> Refresh(RefreshRequestDto request, CancellationToken cancellationToken) => default!;

    [HttpPost]
    //#if (captcha == "reCaptcha")
    [NoRetryPolicy] // Please note that retrying requests with Google reCaptcha will not work, as the Google verification mechanism only accepts a captcha response once.
    //#endif
    Task SignUp(SignUpRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task<SignInResponseDto> SignIn(SignInRequestDto request, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task SendTwoFactorToken(SignInRequestDto request, CancellationToken cancellationToken);

    [HttpPost("{?returnUrl}")]
    Task SendOtp(IdentityRequestDto request, string? returnUrl = null, CancellationToken cancellationToken = default);

    [HttpGet("{?provider,returnUrl,localHttpPort}")]
    Task<string> GetSocialSignInUri(string provider, string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default);
}
