using System.Text;
using System.Text.Encodings.Web;
using Humanizer;
using QRCoder;
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
public partial class UserController : AppControllerBase, IUserController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private IUserStore<User> userStore = default!;

    [AutoInject] private SmsService smsService = default!;

    [AutoInject] private EmailService emailService = default!;

    [AutoInject] private UrlEncoder urlEncoder = default!;

    [HttpGet]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        return user.Map();
    }

    [HttpGet]
    public async Task<List<UserSessionDto>> GetUserSessions(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        return user.Sessions
            .Select(us =>
            {
                var dto = us.Map();

                var lastSeenDateTime = us.RenewedOn ?? us.StartedOn;

                dto.LastSeenOn = DateTimeOffset.UtcNow - lastSeenDateTime < TimeSpan.FromMinutes(5)
                                 ? Localizer[nameof(AppStrings.Online)]
                                 : DateTimeOffset.UtcNow - lastSeenDateTime < TimeSpan.FromMinutes(15)
                                    ? Localizer[nameof(AppStrings.Recently)]
                                    : lastSeenDateTime.Humanize(culture: CultureInfo.CurrentUICulture);

                dto.IsValid = DateTimeOffset.UtcNow - lastSeenDateTime < AppSettings.Identity.RefreshTokenExpiration;

                return dto;
            })
        .ToList();
    }

    [HttpPost]
    public async Task SignOut(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        var currentSessionId = Guid.Parse(User.FindFirstValue("session-id")!);

        user.Sessions = user.Sessions.Where(s => s.SessionUniqueId != currentSessionId).ToList();

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        SignOut();
    }

    [HttpPost("{id}")]
    public async Task RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        var currentSessionId = Guid.Parse(User.FindFirstValue("session-id")!);

        if (id == currentSessionId)
            throw new BadRequestException(); // "Call SignOut instead"

        var currentSession = user.Sessions.SingleOrDefault(s => s.SessionUniqueId == currentSessionId)
            ?? throw new ResourceNotFoundException();

        var revokeUserSessionsDelay = (DateTimeOffset.Now - currentSession.StartedOn) - AppSettings.Identity.RevokeUserSessionsDelay;

        if (revokeUserSessionsDelay < TimeSpan.Zero)
            throw new BadRequestException(Localizer[nameof(AppStrings.WaitForRevokeSessionDelay), revokeUserSessionsDelay.Humanize(culture: CultureInfo.CurrentUICulture)]);

        user.Sessions = user.Sessions.Where(s => s.SessionUniqueId != id).ToList();

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPut]
    public async Task<UserDto> Update(EditUserDto userDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        userDto.Patch(user);

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        return await GetCurrentUser(cancellationToken);
    }

    [HttpPost]
    public async Task ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        if (await userManager.IsLockedOutAsync(user!))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        var result = await userManager.ChangePasswordAsync(user!, request.OldPassword!, request.NewPassword!);

        if (result.Succeeded is false)
        {
            await userManager.AccessFailedAsync(user!);

            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
        }
    }

    [HttpPost]
    public async Task ChangeUserName(ChangeUserNameRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
        var result = await userManager.SetUserNameAsync(user!, request.UserName);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendChangeEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var resendDelay = (DateTimeOffset.Now - user!.EmailTokenRequestedOn) - AppSettings.Identity.EmailTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForEmailTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        user.EmailTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"ChangeEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"));
        var link = new Uri(HttpContext.Request.GetWebClientUrl(), $"{Urls.ProfilePage}?email={Uri.EscapeDataString(request.Email!)}&emailToken={Uri.EscapeDataString(token)}&culture={CultureInfo.CurrentUICulture.Name}");

        await emailService.SendEmailToken(user, request.Email!, token, link, cancellationToken);
    }

    [HttpPost]
    public async Task ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var expired = (DateTimeOffset.Now - user!.EmailTokenRequestedOn) > AppSettings.Identity.EmailTokenLifetime;

        if (expired)
            throw new BadRequestException();

        var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"ChangeEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"), request.Token!);

        if (tokenIsValid is false)
            throw new BadRequestException();

        await ((IUserEmailStore<User>)userStore).SetEmailAsync(user, request.Email, cancellationToken);
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.EmailTokenRequestedOn = null; // invalidates email token
        var updateResult = await userManager.UpdateAsync(user);
        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendChangePhoneNumberToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var resendDelay = (DateTimeOffset.Now - user!.PhoneNumberTokenRequestedOn) - AppSettings.Identity.PhoneNumberTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForPhoneNumberTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        user.PhoneNumberTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user!, request.PhoneNumber!);

        await smsService.SendSms(Localizer[nameof(AppStrings.ChangePhoneNumberTokenSmsText), token], request.PhoneNumber!, cancellationToken);
    }

    [HttpPost]
    public async Task ChangePhoneNumber(ChangePhoneNumberRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var result = await userManager.ChangePhoneNumberAsync(user!, request.PhoneNumber!, request.Token!);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpDelete]
    public async Task Delete(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var result = await userManager.DeleteAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPost, Route("~/api/[controller]/2fa")]
    public async Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ResourceNotFoundException();

        if (request.Enable is true)
        {
            if (request.ResetSharedKey)
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaResetSharedKeyError)]);
            else if (string.IsNullOrEmpty(request.TwoFactorCode))
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaEmptyCodeError)]);
            else if (await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, request.TwoFactorCode) is false)
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaInvalidCodeError)]);

            await userManager.SetTwoFactorEnabledAsync(user, true);
        }
        else if (request.Enable is false || request.ResetSharedKey)
        {
            await userManager.SetTwoFactorEnabledAsync(user, false);
        }

        if (request.ResetSharedKey)
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
        }

        string[]? recoveryCodes = null;
        if (request.ResetRecoveryCodes || (request.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0))
        {
            var recoveryCodesEnumerable = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            recoveryCodes = recoveryCodesEnumerable?.ToArray();
        }

        //if (tfaRequest.ForgetMachine)
        //{
        //    await signInManager.ForgetTwoFactorClientAsync();
        //}

        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(unformattedKey))
            {
                throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
            }
        }

        var sharedKey = FormatKey(unformattedKey);
        var authenticatorUri = GenerateQrCodeUri(user.DisplayName!, unformattedKey);

        var qrCodeBase64 = "";
        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (isTwoFactorEnabled is false)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new Base64QRCode(qrCodeData);
            qrCodeBase64 = qrCode.GetGraphic(20);
        }

        return new TwoFactorAuthResponseDto
        {
            SharedKey = sharedKey,
            AuthenticatorUri = authenticatorUri,
            RecoveryCodes = recoveryCodes,
            RecoveryCodesLeft = recoveryCodes?.Length ?? await userManager.CountRecoveryCodesAsync(user),
            IsTwoFactorEnabled = isTwoFactorEnabled,
            //IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user),
            QrCode = qrCodeBase64
        };
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private const string AUTHENTICATOR_URI_FORMAT = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private string GenerateQrCodeUri(string user, string unformattedKey)
    {
        return string.Format(CultureInfo.InvariantCulture,
        AUTHENTICATOR_URI_FORMAT,
        urlEncoder.Encode("bit platform Boilerplate"),
                             urlEncoder.Encode(user),
                             unformattedKey);
    }
}
