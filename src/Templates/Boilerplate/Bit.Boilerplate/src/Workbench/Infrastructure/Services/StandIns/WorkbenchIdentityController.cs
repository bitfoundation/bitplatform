using System.Text.Json;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Shared.Infrastructure.Exceptions;
using Microsoft.Extensions.Localization;

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// The stand-in for the app's <see cref="IIdentityController"/>, for what a signed-in page asks of it: confirming it's the
/// user with a code or a passkey (the refresh the app's ElevatedAccessModal makes), and the passkey ceremony's options and
/// its check. A code is any six digits, as no message carries one here; a passkey always passes.
/// </summary>
public sealed class WorkbenchIdentityController(WorkbenchScenarioState scenarioState,
                                                WorkbenchElevation elevation,
                                                IAuthTokenProvider tokenProvider,
                                                IStringLocalizer<AppStrings> localizer)
    : WorkbenchStandIn(scenarioState, "identity"), IIdentityController
{
    public async Task<TokenResponseDto> Refresh(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        await ScenarioState.Save(cancellationToken);

        if (request.ElevatedAccessToken is { } code)
        {
            if (code.Length is not 6 || code.All(char.IsAsciiDigit) is false)
                throw new BadRequestException(localizer[nameof(AppStrings.InvalidToken)]);

            elevation.Confirm();
        }
        else if (request.WebAuthnClientResponse is not null)
        {
            elevation.Confirm();
        }

        return new()
        {
            TokenType = "Bearer",
            AccessToken = await tokenProvider.GetAccessToken(),
            RefreshToken = WorkbenchViewStorage.RefreshToken,
            ExpiresIn = (long)TimeSpan.FromHours(1).TotalSeconds
        };
    }

    /// <summary>Options the Workbench's passkey stand-in reads none of (See WorkbenchWebAuthnService).</summary>
    public Task<JsonElement> GetWebAuthnAssertionOptions(WebAuthnAssertionOptionsRequestDto request, CancellationToken cancellationToken)
        => Task.FromResult(JsonSerializer.SerializeToElement(new { challenge = "workbench" }));

    public Task<JsonElement> VerifyWebAuthAssertion(JsonElement clientResponse, CancellationToken cancellationToken)
        => Task.FromResult(clientResponse);

    public Task SendConfirmEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task<TokenResponseDto> ConfirmEmail(ConfirmEmailRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task SendConfirmPhoneToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task<TokenResponseDto> ConfirmPhone(ConfirmPhoneRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task SendResetPasswordToken(SendResetPasswordTokenRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task RevokeRefreshToken(RefreshTokenRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task SignUp(SignUpRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task<SignInResponseDto> SignIn(SignInRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task SendTwoFactorToken(SignInRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task SendOtp(IdentityRequestDto request, string? returnUrl = null, CancellationToken cancellationToken = default) => throw NotUsedByTheWorkbench();

    public Task<string> GetExternalSignInUri(string provider, string? returnUrl = null, int? localHttpPort = null, string? localHttpNonce = null, CancellationToken cancellationToken = default)
        => throw NotUsedByTheWorkbench();

    public Task<SignInResponseDto> VerifyWebAuthAndSignIn(VerifyWebAuthnAndSignInRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task VerifyWebAuthAndSendTwoFactorToken(JsonElement clientResponse, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task<string[]> GetSupportedExternalAuthSchemes(CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();
}
