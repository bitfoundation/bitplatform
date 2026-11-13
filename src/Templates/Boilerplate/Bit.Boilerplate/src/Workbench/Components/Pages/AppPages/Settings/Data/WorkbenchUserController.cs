//+:cnd:noEmit
using System.Text.Json;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;
//#if (multitenant == true)
using Boilerplate.Shared.Features.Tenants.Dtos;
//#endif
using Boilerplate.Shared.Infrastructure.Exceptions;
using Microsoft.Extensions.Localization;
using QRCoder;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The stand-in for the app's <see cref="IUserController"/>, on the view's <see cref="SettingsSampleAccount"/>: everything the
/// settings page asks of the server, answered the way the server answers (UserController). A code the app asks the user
/// for, such as one sent to their new email, is any six digits, as no message carries one here.
/// </summary>
public sealed class WorkbenchUserController(WorkbenchScenarioState scenarioState,
                                            SettingsSampleAccount account,
                                            WorkbenchScenarioSignOut signOut,
                                            IStringLocalizer<AppStrings> localizer)
    : WorkbenchStandIn(scenarioState, "settings"), IUserController
{
    public Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
        => Load("user", () => account.User, () => account.NewUser, cancellationToken);

    public Task<List<UserSessionDto>> GetUserSessions(CancellationToken cancellationToken)
        => Load("sessions", () => account.Sessions, () => account.Sessions.Where(session => session.Id == WorkbenchClaims.CurrentSessionId).ToList(), cancellationToken);

    public Task<UserDto> Update(EditUserRequestDto userDto, CancellationToken cancellationToken) => Save(() => account.ChangeUser(user =>
    {
        user.FullName = userDto.FullName;
        user.Gender = userDto.Gender;
        user.BirthDate = userDto.BirthDate;
    }), cancellationToken);

    public Task ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken) => Save(() => { }, cancellationToken);

    public Task SendChangeEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken) => Save(() => { }, cancellationToken);

    public Task ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken) => Save(() =>
    {
        EnsureCode(request.Token);
        account.ChangeUser(user => user.Email = request.Email);
    }, cancellationToken);

    public Task SendChangePhoneNumberToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken) => Save(() => { }, cancellationToken);

    public Task ChangePhoneNumber(ChangePhoneNumberRequestDto request, CancellationToken cancellationToken) => Save(() =>
    {
        EnsureCode(request.Token);
        account.ChangeUser(user => user.PhoneNumber = request.PhoneNumber);
    }, cancellationToken);

    public Task RevokeSession(Guid id, CancellationToken cancellationToken)
        => Save(() => account.RemoveSession(id), cancellationToken);

    /// <summary>The account is gone: the app signs the device out next, which here signs the view out.</summary>
    public Task Delete(CancellationToken cancellationToken) => Save(() => { }, cancellationToken);

    public async Task SignOut(CancellationToken cancellationToken)
    {
        await ScenarioState.Save(cancellationToken);
        signOut.SignOut();
    }

    /// <summary>The server's two-factor endpoint (UserController.TwoFactorAuth): the empty request is a read.</summary>
    public async Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken)
    {
        var isRead = request.Enable is null && request.ResetSharedKey is false && request.ResetRecoveryCodes is false;

        if (isRead)
            return await Load("two-factor", () => Answer(null), () => Answer(null), cancellationToken);

        return await Save(() =>
        {
            string[]? recoveryCodes = null;
            var isEnabled = account.IsTwoFactorEnabled;
            var key = account.AuthenticatorKey;
            var codesLeft = account.RecoveryCodesLeft;

            if (request.Enable is true)
            {
                if (request.ResetSharedKey)
                    throw new BadRequestException(localizer[nameof(AppStrings.TfaResetSharedKeyError)]);

                if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                    throw new BadRequestException(localizer[nameof(AppStrings.TfaEmptyCodeError)]);

                if (IsCode(request.TwoFactorCode) is false)
                    throw new BadRequestException(localizer[nameof(AppStrings.TfaInvalidCodeError)]);

                isEnabled = true;
            }
            else if (request.Enable is false || request.ResetSharedKey)
            {
                isEnabled = false;
            }

            if (request.ResetSharedKey)
            {
                key = NewKey();
            }

            if (request.ResetRecoveryCodes || (request.Enable is true && codesLeft is 0))
            {
                recoveryCodes = [.. Enumerable.Range(0, 10).Select(_ => $"{Random.Shared.Next(10000, 99999)}-{Random.Shared.Next(10000, 99999)}")];
                codesLeft = recoveryCodes.Length;
            }

            account.ChangeTwoFactor(isEnabled, key, codesLeft);

            return Answer(recoveryCodes);
        }, cancellationToken);
    }

    public Task<ElevatedAccessTokenSentDto> SendElevatedAccessToken(CancellationToken cancellationToken)
        => Save(() => new ElevatedAccessTokenSentDto { SentToEmail = account.User.Email is not null, SentToPhoneNumber = account.User.PhoneNumber is not null }, cancellationToken);

    /// <summary>Options the Workbench's passkey stand-in reads none of (See WorkbenchWebAuthnService).</summary>
    public Task<JsonElement> GetWebAuthnCredentialOptions(CancellationToken cancellationToken)
        => Task.FromResult(JsonSerializer.SerializeToElement(new { challenge = "workbench" }));

    public Task CreateWebAuthnCredential(JsonElement attestationResponse, CancellationToken cancellationToken) => Save(() => account.ChangePasskey(true), cancellationToken);

    public Task DeleteWebAuthnCredential(JsonElement clientResponse, CancellationToken cancellationToken) => Save(() => account.ChangePasskey(false), cancellationToken);

    //#if (signalR == true || notification == true)
    public Task SetNotificationEnabled(bool enabled, CancellationToken cancellationToken) => ScenarioState.Save(cancellationToken);
    //#endif

    public Task UpdateSession(UpdateUserSessionRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task ChangeUserName(ChangeUserNameRequestDto request, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    //#if (multitenant == true)
    public Task<List<TenantDto>> GetTenants(CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();

    public Task LeaveTenant(Guid tenantId, CancellationToken cancellationToken) => throw NotUsedByTheWorkbench();
    //#endif

    /// <summary>What the server answers: the enrolment material only while two-factor is off, and its QR code made the same way.</summary>
    private TwoFactorAuthResponseDto Answer(string[]? recoveryCodes)
    {
        var isEnabled = account.IsTwoFactorEnabled;
        var uri = isEnabled ? "" : $"otpauth://totp/{Uri.EscapeDataString("bit platform Boilerplate")}:{Uri.EscapeDataString(account.User.DisplayName ?? "")}?secret={account.AuthenticatorKey}&issuer={Uri.EscapeDataString("bit platform Boilerplate")}&digits=6";

        return new()
        {
            SharedKey = isEnabled ? "" : account.FormattedKey,
            AuthenticatorUri = uri,
            RecoveryCodes = recoveryCodes,
            RecoveryCodesLeft = recoveryCodes?.Length ?? account.RecoveryCodesLeft,
            IsTwoFactorEnabled = isEnabled,
            QrCode = isEnabled ? "" : QrCodeOf(uri)
        };
    }

    private static string QrCodeOf(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new Base64QRCode(data).GetGraphic(20);
    }

    private static string NewKey() => new([.. Enumerable.Range(0, 16).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"[Random.Shared.Next(32)])]);

    private static bool IsCode(string? code) => code is { Length: 6 } && code.All(char.IsAsciiDigit);

    private void EnsureCode(string? code)
    {
        if (IsCode(code) is false)
            throw new BadRequestException(localizer[nameof(AppStrings.InvalidToken)]);
    }
}
