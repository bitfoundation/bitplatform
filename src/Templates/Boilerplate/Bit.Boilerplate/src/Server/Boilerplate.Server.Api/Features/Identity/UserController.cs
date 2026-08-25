//+:cnd:noEmit
using System.Text.Encodings.Web;
using QRCoder;
using Microsoft.AspNetCore.Cors;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;
//#endif
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

namespace Boilerplate.Server.Api.Features.Identity;

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class UserController : AppControllerBase, IUserController
{
    [AutoInject] private UrlEncoder urlEncoder = default!;
    [AutoInject] private PhoneService phoneService = default!;
    [AutoInject] private IdentityEmailService emailService = default!;
    [AutoInject] private IUserStore<User> userStore = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private IHostEnvironment hostEnvironment = default!;
    [AutoInject] private SignInManager<User> signInManager = default!;
    [AutoInject] private IUserEmailStore<User> userEmailStore = default!;

    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif

    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    [HttpGet]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        return user.Map();
    }

    [HttpGet, EnableQuery]
    public IQueryable<UserSessionDto> GetUserSessions()
    {
        var userId = User.GetUserId();

        return DbContext.UserSessions
            .Where(us => us.UserId == userId)
            .Project()
            .OrderByDescending(us => us.RenewedOn);
    }

    [HttpPost, EnableCors("CorsWithCredentials" /* Required for Cookies.Delete */)]
    public async Task SignOut(CancellationToken cancellationToken)
    {
        var currentSessionId = User.GetSessionId();

        var userSession = await DbContext.UserSessions
            .FirstOrDefaultAsync(us => us.Id == currentSessionId, cancellationToken) ?? throw new ResourceNotFoundException().WithData("Reason", "User session not found.");

        DbContext.UserSessions.Remove(userSession);
        await DbContext.SaveChangesAsync(cancellationToken);

        await signInManager.SignOutAsync();

        if (IsWebPlatformRequest() is false)
            return;

        HttpContext.Response.Cookies.Delete("access_token", BuildAccessTokenCookieOptions());
    }

    [HttpPost("{id}"), Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var currentSessionId = User.GetSessionId();

        if (id == currentSessionId)
            throw new BadRequestException(); // "Call SignOut instead"

        var userSession = await DbContext.UserSessions
            .FirstOrDefaultAsync(us => us.Id == id && us.UserId == userId, cancellationToken) ?? throw new ResourceNotFoundException().WithData("Reason", "User session not found.");

        DbContext.UserSessions.Remove(userSession);
        await DbContext.SaveChangesAsync(cancellationToken);

        //#if (signalR == true)
        // Check out AppHub's comments for more info.
        if (userSession.SignalRConnectionId is not null)
        {
            await appHubContext.Clients.Client(userSession.SignalRConnectionId)
                .Publish(SharedAppMessages.SESSION_REVOKED, null, cancellationToken);
        }
        //#endif
    }

    [HttpPost, EnableCors("CorsWithCredentials" /* Required for Cookies.Append */)]
    public async Task UpdateSession(UpdateUserSessionRequestDto request, CancellationToken cancellationToken)
    {
        // UpdateSession gets called after SignIn, Refresh and client app initialization to update user session info,
        // example scenario would be when user restarts the app after an update or after changing device settings like language.
        // so in server side, we always have the latest info about the user session.

        var affectedRows = await DbContext.UserSessions.Where(us => us.Id == User.GetSessionId()).ExecuteUpdateAsync(us =>
            us.SetProperty(x => x.AppVersion, request.AppVersion)
                .SetProperty(x => x.DeviceInfo, request.DeviceInfo)
                .SetProperty(x => x.PlatformType, request.PlatformType)
                .SetProperty(x => x.CultureName, request.CultureName), cancellationToken);

        if (affectedRows == 0)
            throw new ResourceNotFoundException();

        // access_token's value must be set to cookies for pre-rendering scenarios.
        // But during SignIn/Refresh calls, the cookie can't be set because the access_token value is not available yet.
        // UpdateSession is a good place to set the access token cookie for web clients.

        if (IsWebPlatformRequest() is false)
            return;

        var accessToken = HttpContext.GetAccessToken() ?? throw new InvalidOperationException("Access token not found.");
        DateTimeOffset expirationTime = DateTimeOffset.FromUnixTimeSeconds(User.GetClaimValue<long>("exp"));

        var cookieOptions = BuildAccessTokenCookieOptions();
        cookieOptions.Expires = expirationTime;

        Response.Cookies.Append("access_token", accessToken, cookieOptions);
    }

    [HttpPut]
    public async Task<UserDto> Update(EditUserRequestDto userDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        userDto.Patch(user);

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        var updatedUser = await GetCurrentUser(cancellationToken);

        //#if (signalR == true)
        // Notify other sessions of the user that user's info has been updated, so they'll update their UI.
        var currentUserSessionId = User.GetSessionId();
        var userSessionIdsExceptCurrentUserSessionId = await DbContext.UserSessions
            .Where(us => us.UserId == user.Id && us.Id != currentUserSessionId && us.SignalRConnectionId != null)
            .Select(us => us.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
        await appHubContext.Clients.Clients(userSessionIdsExceptCurrentUserSessionId).Publish(SharedAppMessages.PROFILE_UPDATED, updatedUser, cancellationToken);
        //#endif

        return updatedUser;
    }

    [HttpPost]
    public async Task ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        if (await userManager.IsLockedOutAsync(user!))
        {
            var tryAgainIn = (user!.LockoutEnd! - TimeProvider.GetUtcNow()).Value;
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), tryAgainIn.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", tryAgainIn);
        }

        var result = await userManager.ChangePasswordAsync(user!, request.OldPassword!, request.NewPassword!);

        if (result.Succeeded is false)
        {
            await userManager.AccessFailedAsync(user!);

            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
        }
    }

    /// <summary>
    /// Changing an account identifier (user name, e-mail, phone) takes TWO proofs, and this policy is the first of them:
    /// <list type="number">
    /// <item><b>That you still hold the current account.</b> <see cref="AuthPolicies.ELEVATED_ACCESS"/> is only granted
    /// after the user quotes back a 6-digit code that <see cref="SendElevatedAccessToken"/> sent to the identifiers the
    /// account ALREADY has (its confirmed e-mail / phone). Someone holding only a stolen access token cannot produce it.</item>
    /// <item><b>That you hold the new identifier.</b> <see cref="SendChangeEmailToken"/> /
    /// <see cref="SendChangePhoneNumberToken"/> then send a second code to the NEW address, and
    /// <see cref="ChangeEmail"/> / <see cref="ChangePhoneNumber"/> only apply the change once it comes back.</item>
    /// </list>
    /// Without the first proof the second one is worthless: the attacker picks the new address himself, so his own inbox
    /// is the only thing he has to control - and afterwards every recovery path (password reset, magic-link sign-in, OTP)
    /// points at him. Note the policy sits on the <c>Send*</c> half, so following the e-mailed link to finish the change
    /// does not prompt for a second code. <c>ChangeUserName</c> has no second proof at all, which is why it needs this one.
    /// </summary>
    [HttpPost, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task ChangeUserName(ChangeUserNameRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
        var result = await userManager.SetUserNameAsync(user!, request.UserName);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    /// <inheritdoc cref="ChangeUserName"/>
    [HttpPost, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendChangeEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        await EnsureIdentifierIsAvailable(new() { Email = request.Email }, user!.Id, cancellationToken);

        // How long is still left before another token may be requested. Positive while the caller has to wait, which is
        // what "TryAgainIn" means everywhere else (the lockout exceptions attach the same key as a positive duration).
        var tryAgainIn = AppSettings.Identity.EmailTokenLifetime - (TimeProvider.GetUtcNow() - user.EmailTokenRequestedOn);

        if (tryAgainIn > TimeSpan.Zero)
            throw new TooManyRequestsException(Localizer[nameof(AppStrings.WaitForEmailTokenRequestResendDelay), tryAgainIn.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", tryAgainIn.Value);

        user.EmailTokenRequestedOn = TimeProvider.GetUtcNow();
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(
            user,
            TokenOptions.DefaultPhoneProvider,
            FormattableString.Invariant($"ChangeEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"));

        var link = new Uri(
            HttpContext.Request.GetWebAppUrl(),
            $"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}?email={Uri.EscapeDataString(request.Email!)}&emailToken={Uri.EscapeDataString(token)}&culture={CultureInfo.CurrentUICulture.Name}");

        await emailService.SendEmailToken(user, request.Email!, token, link, cancellationToken);
    }

    [HttpPost]
    public async Task ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var expired = user!.EmailTokenRequestedOn is null ||
                      (TimeProvider.GetUtcNow() - user.EmailTokenRequestedOn.Value) > AppSettings.Identity.EmailTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken));

        var tokenIsValid = await userManager.VerifyUserTokenAsync(
            user,
            TokenOptions.DefaultPhoneProvider,
            FormattableString.Invariant($"ChangeEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"),
            request.Token!);

        if (tokenIsValid is false)
            throw new BadRequestException(nameof(AppStrings.InvalidToken));

        await EnsureIdentifierIsAvailable(new() { Email = request.Email }, user.Id, cancellationToken);

        await userEmailStore.SetEmailAsync(user, request.Email, cancellationToken);
        // The token proved the caller controls the new address, so it is confirmed - and the security stamp has to rotate,
        // otherwise changing the account's recovery address leaves every other session alive. UserManager.ChangeEmailAsync
        // does both; the raw store call this endpoint uses (so it can keep the app's own token scheme) does neither.
        await ((IUserEmailStore<User>)userStore).SetEmailConfirmedAsync(user, true, cancellationToken);
        await userManager.UpdateSecurityStampAsync(user);

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.EmailTokenRequestedOn = null; // invalidates email token
        var updateResult = await userManager.UpdateAsync(user);

        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    /// <inheritdoc cref="ChangeUserName"/>
    [HttpPost, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task SendChangePhoneNumberToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        await EnsureIdentifierIsAvailable(new() { PhoneNumber = request.PhoneNumber }, user!.Id, cancellationToken);

        // Positive while the caller still has to wait, same as SendChangeEmailToken.
        var tryAgainIn = AppSettings.Identity.PhoneNumberTokenLifetime - (TimeProvider.GetUtcNow() - user.PhoneNumberTokenRequestedOn);

        if (tryAgainIn > TimeSpan.Zero)
            throw new TooManyRequestsException(Localizer[nameof(AppStrings.WaitForPhoneNumberTokenRequestResendDelay), tryAgainIn.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", tryAgainIn.Value);

        user.PhoneNumberTokenRequestedOn = TimeProvider.GetUtcNow();
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user!, request.PhoneNumber!);

        var message = Localizer[nameof(AppStrings.ChangePhoneNumberTokenShortText), token];
        var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;

        await phoneService.SendSms(smsMessage, request.PhoneNumber!);
    }

    [HttpPost]
    public async Task ChangePhoneNumber(ChangePhoneNumberRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var expired = user!.PhoneNumberTokenRequestedOn is null ||
                      (TimeProvider.GetUtcNow() - user.PhoneNumberTokenRequestedOn.Value) > AppSettings.Identity.PhoneNumberTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken));

        await EnsureIdentifierIsAvailable(new() { PhoneNumber = request.PhoneNumber }, user.Id, cancellationToken);

        var result = await userManager.ChangePhoneNumberAsync(user, request.PhoneNumber!, request.Token!);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.PhoneNumberTokenRequestedOn = null; // invalidates phone token
        var updateResult = await userManager.UpdateAsync(user);

        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpDelete, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var currentSessionId = User.GetSessionId();

        //#if (signalR == true)
        var userSessionConnectionIds = await DbContext.UserSessions
            .Where(us => us.UserId == userId && us.Id != currentSessionId && us.SignalRConnectionId != null)
            .Select(us => us.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
        //#endif

        await DbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

            await DbContext.UserSessions.Where(us => us.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            // Re-read inside the delegate: on a retry the instance from the failed attempt is still tracked, already
            // marked Deleted and carrying the concurrency stamp it was loaded with, so deleting it again would either
            // fault or run against a stale stamp.
            var userToDelete = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ResourceNotFoundException();

            var result = await userManager.DeleteAsync(userToDelete);

            if (result.Succeeded is false)
                throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

            await transaction.CommitAsync(cancellationToken);
        });

        await signInManager.SignOutAsync();

        //#if (signalR == true)
        // Check out AppHub's comments for more info.
        await appHubContext.Clients.Clients(userSessionConnectionIds).Publish(SharedAppMessages.SESSION_REVOKED, null, cancellationToken);
        //#endif

        if (IsWebPlatformRequest() is false)
            return;

        HttpContext.Response.Cookies.Delete("access_token", BuildAccessTokenCookieOptions());
    }

#pragma warning disable ASP0018
    [HttpPost, Route("~/api/v{v:apiVersion}/[controller]/2fa")]
#pragma warning restore ASP0018
    public async Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ResourceNotFoundException().WithData("Reason", "User not found.");

        // The empty request is a READ - the settings page calls it on every visit to fetch the enrolment material and the
        // recovery-code count - so the endpoint itself can't carry [Authorize(ELEVATED_ACCESS)] without prompting for a
        // code every time the tab is opened. Everything that WEAKENS the second factor is gated here instead. Enabling is
        // excluded because it already requires a valid TOTP code, which is stronger proof than elevation.
        // Note the gate below is NOT what keeps the shared key safe once two factor is on - the read is deliberately
        // ungated, so the response itself withholds the key instead. See the enrolment-material block further down.
        var weakensTwoFactor = request.Enable is false || request.ResetSharedKey || request.ResetRecoveryCodes;

        var elevatedSessionExpiresOn = User.GetElevatedSessionExpiresOn();

        if (weakensTwoFactor && (elevatedSessionExpiresOn is null || elevatedSessionExpiresOn.Value <= TimeProvider.GetUtcNow()))
            throw new ForbiddenException().WithData("Reason", "Changing two factor authentication settings requires elevated access.");

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
            IUserAuthenticatorKeyStore<User> userAuthenticatorKeyStore = (IUserAuthenticatorKeyStore<User>)userStore;
            await userAuthenticatorKeyStore.SetAuthenticatorKeyAsync(user,
                userManager.GenerateNewAuthenticatorKey(), cancellationToken);
            await userStore.UpdateAsync(user, cancellationToken);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(unformattedKey))
            {
                throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
            }
        }

        var sharedKey = "";
        var qrCodeBase64 = "";
        var authenticatorUri = "";
        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (isTwoFactorEnabled is false)
        {
            sharedKey = FormatKey(unformattedKey);
            authenticatorUri = GenerateQrCodeUri(user.DisplayName!, unformattedKey);

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

    [HttpPost]
    public async Task SendElevatedAccessToken(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        // Elevated access token claim gets added to access token upon refresh token request call, so their lifetime would be the same.
        // Positive while the caller still has to wait, same as SendChangeEmailToken.
        var tryAgainIn = AppSettings.Identity.BearerTokenExpiration - (TimeProvider.GetUtcNow() - user!.ElevatedAccessTokenRequestedOn);

        if (tryAgainIn > TimeSpan.Zero)
            throw new TooManyRequestsException(Localizer[nameof(AppStrings.WaitForElevatedAccessTokenRequestResendDelay), tryAgainIn.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", tryAgainIn.Value);

        user.ElevatedAccessTokenRequestedOn = TimeProvider.GetUtcNow();
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var currentUserSessionId = User.GetSessionId();

        var token = await userManager.GenerateUserTokenAsync(
            user,
            TokenOptions.DefaultPhoneProvider,
            FormattableString.Invariant($"ElevatedAccess:{currentUserSessionId},{user.ElevatedAccessTokenRequestedOn?.ToUniversalTime()}"));

        List<Task> sendMessagesTasks = [];

        var message = Localizer[nameof(AppStrings.ElevatedAccessTokenShortText), token].ToString();

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendElevatedAccessToken(user, token, cancellationToken));
        }

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!));
        }

        if (user.TwoFactorEnabled || (user.EmailConfirmed is false && user.PhoneNumberConfirmed is false /* Users signed-in through external sign-in */))
        {
            //#if (signalR == true)
            // Check out AppHub's comments for more info.
            var userSessionIdsExceptCurrentUserSessionId = await DbContext.UserSessions
                .Where(us => us.NotificationStatus == UserSessionNotificationStatus.Allowed && us.UserId == user.Id && us.Id != currentUserSessionId && us.SignalRConnectionId != null)
                .Select(us => us.SignalRConnectionId!)
                .ToArrayAsync(cancellationToken);
            sendMessagesTasks.Add(appHubContext.Clients.Clients(userSessionIdsExceptCurrentUserSessionId).SendAsync(SharedAppMessages.SHOW_MESSAGE, message, null, cancellationToken));
            //#endif

            //#if (notification == true)
            sendMessagesTasks.Add(pushNotificationService.RequestPush(new()
            {
                Message = message,
                UserRelatedPush = true
            }, customSubscriptionFilter: us => us.UserSession!.UserId == user.Id && us.UserSessionId != currentUserSessionId, cancellationToken: cancellationToken));
            //#endif
        }

        await Task.WhenAll(sendMessagesTasks);
    }

    //#if (signalR == true || notification == true)
    [HttpPost("{userSessionId}")]
    public async Task<UserSessionNotificationStatus> ToggleNotification(Guid userSessionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var userSession = await DbContext.UserSessions
            .FirstOrDefaultAsync(us => us.Id == userSessionId && us.UserId == userId, cancellationToken) ?? throw new ResourceNotFoundException().WithData("Reason", "User session not found.");

        userSession.NotificationStatus = userSession.NotificationStatus is UserSessionNotificationStatus.NotConfigured ? UserSessionNotificationStatus.Allowed :
            userSession.NotificationStatus is UserSessionNotificationStatus.Allowed ? UserSessionNotificationStatus.Muted : UserSessionNotificationStatus.Allowed;

        await DbContext.SaveChangesAsync(cancellationToken);

        if (userSession.NotificationStatus is UserSessionNotificationStatus.Allowed)
        {
            //#if (notification == true)
            await pushNotificationService.RequestPush(new()
            {
                Message = Localizer[nameof(AppStrings.TestNotificationMessage1)],
                UserRelatedPush = true
            }, customSubscriptionFilter: us => us.UserSessionId == userSessionId, cancellationToken: cancellationToken);
            //#endif
            //#if (signalR == true)
            if (userSession.SignalRConnectionId != null)
            {
                await appHubContext.Clients.Client(userSession.SignalRConnectionId).SendAsync(SharedAppMessages.SHOW_MESSAGE, (string)Localizer[nameof(AppStrings.TestNotificationMessage2)], null, cancellationToken);
            }
            //#endif
        }

        return userSession.NotificationStatus;
    }
    //#endif

    //#if (multitenant == true)
    [HttpGet]
    public async Task<List<TenantDto>> GetTenants(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        // Global admins get all active tenants, so they can switch into any of them; regular users only get the tenants they
        // belong to, including not accepted (invited) ones, so they can accept the invitation by switching into them.
        var query = User.HasFeature(AppFeatures.Management.Tenants_Manage_Global)
            ? DbContext.Tenants.Where(t => t.IsActive)
            : DbContext.Tenants.Where(t => t.IsActive && t.Users.Any(tu => tu.UserId == userId));

        // The membership is carried over so the client can tell the three states apart: null = the caller has no
        // membership at all (only reachable for a global admin, who is listed every active tenant), false = invited but
        // not accepted yet (Accept), true = accepted (Switch). Collapsing "no membership" into false would put a Leave
        // action on tenants the caller was never a member of, which the server can only answer with a 404.
        var tenants = await query
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                Tenant = t,
                IsMember = t.Users.Any(tu => tu.UserId == userId),
                AcceptedOn = t.Users.Where(tu => tu.UserId == userId).Select(tu => tu.AcceptedOn).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return [.. tenants.Select(t =>
        {
            var dto = t.Tenant.Map();
            dto.CurrentUserHasAcceptedThisTenantInvitation = t.IsMember ? t.AcceptedOn is not null : null;
            return dto;
        })];
    }

    /// <summary>
    /// Setting AcceptedOn to null hides the user from the tenant's users list and prevents auto signing into that tenant,
    /// but the user can re-join later by switching into it again, as long as the TenantUser record exists.
    /// <para>
    /// Leaving a membership that is ALREADY pending deletes it instead, which is how an invitation gets declined. There
    /// is no other way to get rid of one: setting AcceptedOn to null again changes nothing while still reporting success,
    /// and the tenant admin cannot remove it either, because UserManagementController.EnsureUserIsInCurrentTenant only
    /// reaches accepted memberships. Without this, anyone holding Tenant_Manage could pin a tenant card carrying text of
    /// their choosing into an arbitrary user's tenant list, permanently.
    /// </para>
    /// </summary>
    [HttpPost("{tenantId}"), Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task LeaveTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var membership = await DbContext.TenantUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(tu => tu.UserId == userId && tu.TenantId == tenantId, cancellationToken)
            ?? throw new ResourceNotFoundException().WithData("Reason", "Tenant user not found.");

        var isDecliningAPendingInvitation = membership.AcceptedOn is null;

        // The tenant claim gets read from the user session during token refresh (See IdentityController.Refresh),
        // so all of the user's sessions that are signed into this tenant get moved to her next tenant (or none).
        var nextTenantId = await DbContext.TenantUsers
            .Where(tu => tu.UserId == userId && tu.TenantId != tenantId && tu.AcceptedOn != null && tu.Tenant!.IsActive)
            .OrderBy(tu => tu.AcceptedOn)
            .Select(tu => (Guid?)tu.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        await DbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

            var membershipToLeave = DbContext.TenantUsers.Where(tu => tu.UserId == userId && tu.TenantId == tenantId);

            if (isDecliningAPendingInvitation)
            {
                await membershipToLeave.ExecuteDeleteAsync(cancellationToken);
            }
            else
            {
                await membershipToLeave.ExecuteUpdateAsync(tu => tu.SetProperty(t => t.AcceptedOn, (DateTimeOffset?)null), cancellationToken);
            }

            await DbContext.UserSessions
                .Where(us => us.UserId == userId && us.TenantId == tenantId)
                .ExecuteUpdateAsync(us => us.SetProperty(s => s.TenantId, nextTenantId), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
    //#endif

    /// <summary>
    /// A missing, duplicated or unrecognised X-App-Platform header means "not a web client", not a server fault.
    /// Every shipped client always sends it (See RequestHeadersDelegatingHandler), but an API consumer need not, and
    /// Enum.Parse(Headers[...].Single()) turned that into a 500 logged at Critical - after the endpoint had already
    /// committed its write. ApiServerExceptionHandler reads the same header defensively.
    /// </summary>
    private bool IsWebPlatformRequest()
    {
        return HttpContext.Request.Headers.TryGetValue("X-App-Platform", out var values)
            && Enum.TryParse<AppPlatformType>(values.FirstOrDefault(), ignoreCase: true, out var appPlatformType)
            && appPlatformType is AppPlatformType.Web;
    }

    /// <summary>
    /// The access token cookie's attributes have to be byte-identical between Append and Delete, otherwise the browser
    /// keeps the old cookie. Built in one place so they cannot drift.
    /// </summary>
    /// <remarks>
    /// Why the access token is in a cookie at all: the client keeps it in storage and sends it as a Bearer header, but
    /// PRE-RENDERING happens before any of that exists, so a cookie the browser attaches on its own is the only way
    /// <c>ServerSideAuthTokenProvider</c> can tell who the user is on the first response.
    /// <para>
    /// That is also why the Domain is the WEB APP's host and not the api's - pre-rendering runs on the web app. Under
    /// <c>api == Standalone</c> the two are different hosts, and a host-only cookie (no Domain) would stay on the api.
    /// </para>
    /// <para>
    /// The constraint this puts on a deployment: the api host must domain-match the web app host, or the browser
    /// DISCARDS the cookie (RFC 6265 5.3) and every page silently pre-renders as anonymous. Web <c>myapp.com</c> +
    /// api <c>api.myapp.com</c> works; web <c>app.myapp.com</c> + api <c>app-api.myapp.com</c> does not, because those
    /// two are siblings rather than parent and child. The accepted cost is that a cookie carrying a Domain also
    /// reaches every OTHER subdomain of that host - there is no way to scope a cookie to two named hosts.
    /// </para>
    /// </remarks>
    private CookieOptions BuildAccessTokenCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = hostEnvironment.IsDevelopment() is false || Request.IsHttps,
            Path = "/",
            Domain = HttpContext.Request.GetWebAppUrl().Host,
            IsEssential = true
        };
    }

    /// <summary>
    /// UserConfiguration puts unique indexes on Email and PhoneNumber, but IdentityOptions.User.RequireUniqueEmail
    /// defaults to false and UserValidator never checks a phone number at all - so without this the write reaches the
    /// database and comes back as a raw DbUpdateException, i.e. a 500 logged at Critical for something the user can fix.
    /// </summary>
    private async Task EnsureIdentifierIsAvailable(IdentityRequestDto identifier, Guid currentUserId, CancellationToken cancellationToken)
    {
        var owner = await userManager.FindUser(identifier);

        if (owner is not null && owner.Id != currentUserId)
            throw new BadRequestException(Localizer[nameof(AppStrings.DuplicateEmailOrPhoneNumber)]);
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
