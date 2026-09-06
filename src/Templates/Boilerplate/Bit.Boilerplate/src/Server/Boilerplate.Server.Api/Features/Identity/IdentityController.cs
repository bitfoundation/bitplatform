//+:cnd:noEmit
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.BearerToken;
//#if (notification == true)
using Boilerplate.Server.Api.Features.PushNotification;
//#endif

namespace Boilerplate.Server.Api.Features.Identity;

[ApiVersion(1)]
[ApiController, AllowAnonymous]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private IUserStore<User> userStore = default!;
    [AutoInject] private IUserEmailStore<User> userEmailStore = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private SignInManager<User> signInManager = default!;
    [AutoInject] private ILogger<IdentityController> logger = default!;
    [AutoInject] private UserClaimsService userClaimsService = default!;
    [AutoInject] private IUserConfirmation<User> userConfirmation = default!;
    [AutoInject] private IUserPhoneNumberStore<User> userPhoneNumberStore = default!;
    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;
    [AutoInject] private AppUserClaimsPrincipalFactory userClaimsPrincipalFactory = default!;
    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif
    //#if (notification == true)
    [AutoInject] private PushNotificationService pushNotificationService = default!;
    //#endif

    //#if (captcha == "reCaptcha")
    [AutoInject] private GoogleRecaptchaService googleRecaptchaService = default!;
    //#endif

    /// <summary>
    /// By leveraging summary tags in your controller's actions and DTO properties you can make your codes much easier to maintain.
    /// These comments will also be used in swagger/scalar docs and ui.
    /// </summary>
    [HttpPost, EnableRateLimiting(RateLimitOptionsExtensions.IDENTITY)]
    public async Task SignUp(SignUpRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        //#if (captcha == "reCaptcha")
        if (await googleRecaptchaService.Verify(request.GoogleRecaptchaResponse, cancellationToken) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.InvalidGoogleRecaptchaResponse)]);
        //#endif

        // Attempt to locate an existing user using either their email address or phone number. The enforcement of a unique username policy is integral to the aspnetcore identity framework.
        var existingUser = await userManager.FindUser(new() { Email = request.Email, PhoneNumber = request.PhoneNumber });
        if (existingUser is not null)
        {

            if (await userConfirmation.IsConfirmedAsync(userManager, existingUser) is false)
            {
                await SendConfirmationToken(existingUser, request.ReturnUrl, cancellationToken);
                throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]).WithData("UserId", existingUser.Id);
            }
            else
            {
                throw new BadRequestException(Localizer[nameof(AppStrings.DuplicateEmailOrPhoneNumber)]).WithData("UserId", existingUser.Id);
            }
        }

        var userToAdd = new User { LockoutEnabled = true };

        await userStore.SetUserNameAsync(userToAdd, request.UserName!, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Email) is false)
        {
            await userEmailStore.SetEmailAsync(userToAdd, request.Email!, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber) is false)
        {
            await userPhoneNumberStore.SetPhoneNumberAsync(userToAdd, request.PhoneNumber!, cancellationToken);
        }

        await userManager.CreateUserWithDemoRole(userToAdd, request.Password!);

        await SendConfirmationToken(userToAdd, request.ReturnUrl, cancellationToken);
    }

    [HttpPost, Produces<SignInResponseDto>(), EnableRateLimiting(RateLimitOptionsExtensions.IDENTITY)]
    public async Task SignIn(SignInRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);

        var user = await userManager.FindUser(request)
                    ?? await userManager.CreateUserWithDemoRole(request); // Check out SignInModalService for more details

        await SignIn(request, user, cancellationToken);
    }

    private async Task SignIn(SignInRequestDto request, User user, CancellationToken cancellationToken)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        //#if (multitenant == true)
        var tenantId = await GetTenantId(user.Id, cancellationToken);

        if (tenantId is not null)
        {
            userClaimsPrincipalFactory.SetTenantId(tenantId.Value);
        }
        //#endif

        var userSession = await CreateUserSession(user.Id, cancellationToken);

        //#if (multitenant == true)
        userSession.TenantId = tenantId;
        //#endif

        if (user.TwoFactorEnabled)
        {
            userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.ELEVATED_SESSION, NewElevatedSessionExpiresOn().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
        }

        userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.SESSION_ID, userSession.Id.ToString()));

        bool isOtpSignIn = string.IsNullOrWhiteSpace(request.Otp) is false;

        var (signInResult, firstStepAuthenticationMethod) = isOtpSignIn
            ? await signInManager.OtpSignIn(user, request.Otp!)
            : (await signInManager.PasswordSignInAsync(user, request.Password!, isPersistent: false, lockoutOnFailure: true), authenticationMethod: "Password");

        if (signInResult.IsNotAllowed && await userConfirmation.IsConfirmedAsync(userManager, user) is false)
        {
            try
            {
                await SendConfirmationToken(user, request.ReturnUrl, cancellationToken);
            }
            catch (TooManyRequestsException) { }
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]).WithData("UserId", user.Id);
        }

        if (signInResult.IsLockedOut)
            throw UserLockedOutException(user);

        if (signInResult.RequiresTwoFactor)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode) is false)
            {
                signInResult = await TwoFactorSignIn(user, request.TwoFactorCode);

                if (signInResult.IsLockedOut)
                    throw UserLockedOutException(user);
            }
            else
            {
                await Response.WriteAsJsonAsync(new SignInResponseDto { RequiresTwoFactor = true }, cancellationToken);
                return;
            }
        }

        if (signInResult.Succeeded is false)
            throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]).WithData(new() { { "UserId", user.Id }, { "Identifier", request } });

        await DbContext.UserSessions.AddAsync(userSession, cancellationToken);
        user.TwoFactorTokenRequestedOn = null;
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private BadRequestException UserLockedOutException(User user)
    {
        var tryAgainIn = (user.LockoutEnd! - TimeProvider.GetUtcNow()).Value;
        return new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), tryAgainIn.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", tryAgainIn);
    }

    private async Task<Microsoft.AspNetCore.Identity.SignInResult> TwoFactorSignIn(User user, string code)
    {
        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded is false)
        {
            var authenticatorProvider = userManager.Options.Tokens.AuthenticatorTokenProvider;

            result = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, code)
                ? await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultPhoneProvider, code, false, false)
                : await userManager.VerifyTwoFactorTokenAsync(user, authenticatorProvider, code)
                    ? await signInManager.TwoFactorAuthenticatorSignInAsync(code, false, false)
                    : await FailedTwoFactorSignIn(user);
        }

        if (result.Succeeded is true && user.OtpRequestedOn != null)
        {
            await userManager.ResetAccessFailedCountAsync(user);
            user.OtpRequestedOn = null; // invalidates the OTP
            var updateResult = await userManager.UpdateAsync(user);
            if (updateResult.Succeeded is false)
                throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);
        }

        return result;
    }

    /// <summary>
    /// Counts exactly one failed attempt for a two-factor code that matched no provider, mirroring what a single
    /// SignInManager two-factor call would have done.
    /// </summary>
    private async Task<Microsoft.AspNetCore.Identity.SignInResult> FailedTwoFactorSignIn(User user)
    {
        await userManager.AccessFailedAsync(user);

        return await userManager.IsLockedOutAsync(user)
            ? Microsoft.AspNetCore.Identity.SignInResult.LockedOut
            : Microsoft.AspNetCore.Identity.SignInResult.Failed;
    }

    //#if (multitenant == true)
    /// <summary>
    /// When the user signs in, her tenants' FirstOrDefault id gets used as the tenant id claim; if none, the claim doesn't get added at all.
    /// Only accepted memberships of active tenants are considered here; invited users can switch into the tenant
    /// afterwards, which accepts the invitation by setting TenantUser's AcceptedOn.
    /// </summary>
    private async Task<Guid?> GetTenantId(Guid userId, CancellationToken cancellationToken)
    {
        return await DbContext.TenantUsers
            .Where(tu => tu.UserId == userId && tu.AcceptedOn != null && tu.Tenant!.IsActive)
            .OrderBy(tu => tu.AcceptedOn)
            .Select(tu => (Guid?)tu.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }
    //#endif

    /// <summary>
    /// Creates a user session and adds its ID to the access and refresh tokens, but only if the sign-in is successful <see cref="AppUserClaimsPrincipalFactory.SessionClaims"/>
    /// </summary>
    private async Task<UserSession> CreateUserSession(Guid userId, CancellationToken cancellationToken)
    {
        var userSession = new UserSession
        {
            Id = Guid.CreateSequentialGuid(),
            UserId = userId,
            StartedOn = TimeProvider.GetUtcNow().ToUnixTimeSeconds(),
            IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
            //#if (cloudflare == true)
            // Relying on Cloudflare cdn to retrieve address.
            // https://developers.cloudflare.com/rules/transform/managed-transforms/reference/#add-visitor-location-headers
            Address = $"{Request.Headers["cf-ipcountry"]}, {Request.Headers["cf-ipcity"]}"
            //#endif
        };

        await UpdateUserSessionPrivilegeStatus(userSession, cancellationToken);

        return userSession;
    }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.PRIVILEGED_ACCESS"/>
    /// </summary>
    private async Task UpdateUserSessionPrivilegeStatus(UserSession userSession, CancellationToken cancellationToken)
    {
        var userId = userSession.UserId;

        var maxPrivilegedSessionsClaimValues = await userClaimsService.GetClaimValues<int?>(userId, AppClaimTypes.MAX_PRIVILEGED_SESSIONS, cancellationToken);

        var maxPrivilegedSessionsCount = maxPrivilegedSessionsClaimValues.Max() ?? AppSettings.Identity.MaxPrivilegedSessionsCount;

        var hasUnlimitedPrivilegedSessions = maxPrivilegedSessionsClaimValues.Any(v => v is AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS)
            || maxPrivilegedSessionsCount is AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS;

        if (hasUnlimitedPrivilegedSessions)
        {
            maxPrivilegedSessionsCount = AppClaimTypes.UNLIMITED_PRIVILEGED_SESSIONS;
        }

        var isPrivileged = hasUnlimitedPrivilegedSessions ||
            userSession.Privileged is true || // Once session gets privileged, it stays privileged until gets deleted (but see Refresh: switching tenant clears it, so it is re-earned under the new tenant's claims).
            await DbContext.UserSessions.CountAsync(us => us.UserId == userSession.UserId && us.Privileged == true, cancellationToken) < maxPrivilegedSessionsCount;

        userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.PRIVILEGED_SESSION, isPrivileged ? "true" : "false"));
        userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.MAX_PRIVILEGED_SESSIONS, maxPrivilegedSessionsCount.ToString(CultureInfo.InvariantCulture)));

        userSession.Privileged = isPrivileged;
    }

    /// <summary>
    /// The elevated session stays effective only for a short window (the access token's lifetime), after which the
    /// <see cref="AuthPolicies.ELEVATED_ACCESS"/> policy is no longer satisfied even if the (now stale) claim is still present.
    /// </summary>
    private DateTimeOffset NewElevatedSessionExpiresOn() => TimeProvider.GetUtcNow().Add(AppSettings.Identity.BearerTokenExpiration);

    [HttpPost]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        UserSession? userSession = null;

        try
        {
            var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(request.RefreshToken);

            if (refreshTicket?.Principal?.IsAuthenticated() is not true)
                throw new UnauthorizedException().WithData("Reason", "Refresh token is not authenticated.");

            HttpContext.Items[AppClaimTypes.METHOD] = refreshTicket.Principal.GetClaimValue<string?>(AppClaimTypes.METHOD);

            var securityStamp = refreshTicket.Principal.GetClaimValue<string?>("AspNet.Identity.SecurityStamp") ?? throw new UnauthorizedException().WithData("Reason", "Security stamp is missing.");

            var currentSessionId = refreshTicket.Principal.GetSessionId();
            userSession = await DbContext.UserSessions
                .Include(us => us.User)
                .FirstOrDefaultAsync(us => us.Id == currentSessionId, cancellationToken) ?? throw new UnauthorizedException().WithData("UserSessionId", currentSessionId); // User session has been deleted.

            if ((refreshTicket.Properties.ExpiresUtc ?? DateTimeOffset.MinValue) < TimeProvider.GetUtcNow())
                throw new UnauthorizedException().WithData("Reason", "Refresh token is expired.");

            // Refresh token rotation detection: If the refresh token is used more than once, then it means the token has been compromised, so we should reject the request.
            // The first tolerance absorbs the gap between RenewedOn being written here and the replacement token being
            // stamped by AppJwtSecureDataFormat.Protect afterwards. The second one covers a lost rotation response:
            // that client still holds the superseded token, and RetryDelegatingHandler re-sends the request within
            // seconds, so a rotation that just happened is not yet treated as reuse.
            long issuedAtClaimValue = refreshTicket.Principal.GetClaimValue<long>("iat");
            long lastRotatedOn = userSession.RenewedOn ?? userSession.StartedOn;
            long difference = Math.Abs(issuedAtClaimValue - lastRotatedOn);
            if (difference > 30 && (TimeProvider.GetUtcNow().ToUnixTimeSeconds() - lastRotatedOn) > 10)
                throw new UnauthorizedException().WithData("Reason", "Refresh token rotation detected.");

            var user = userSession.User!;

            if (await signInManager.ValidateSecurityStampAsync(userSession.User, securityStamp) is false)
                throw new UnauthorizedException().WithData("Reason", "Security stamp has been updated (for example after 2fa configuration)");

            var elevatedSessionExpiresOn = refreshTicket.Principal.GetElevatedSessionExpiresOn();

            if (string.IsNullOrWhiteSpace(request.ElevatedAccessToken) is false)
            {
                if (await userManager.IsLockedOutAsync(user))
                    throw UserLockedOutException(user);

                var elevatedAccessTokenExpired = user.ElevatedAccessTokenRequestedOn is null ||
                                                 (TimeProvider.GetUtcNow() - user.ElevatedAccessTokenRequestedOn.Value) > AppSettings.Identity.BearerTokenExpiration;

                if (elevatedAccessTokenExpired && user.TwoFactorEnabled is false)
                    throw new BadRequestException(nameof(AppStrings.ExpiredToken)).WithData("UserId", user.Id);

                var tokenIsValid = (elevatedAccessTokenExpired is false
                        && await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"ElevatedAccess:{userSession.Id},{user.ElevatedAccessTokenRequestedOn!.Value.ToUniversalTime()}"), request.ElevatedAccessToken))
                    || await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, request.ElevatedAccessToken);
                if (tokenIsValid is false)
                {
                    await userManager.AccessFailedAsync(user);
                    throw new BadRequestException(nameof(AppStrings.InvalidToken)).WithData("UserId", user.Id);
                }
                else
                {
                    user.ElevatedAccessTokenRequestedOn = null; // invalidates token
                    await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
                    elevatedSessionExpiresOn = NewElevatedSessionExpiresOn();
                }
            }

            //#if (multitenant == true)
            // The tenant claim gets read from the user session (not from the passed refresh token), which is kept in sync
            // by the sign-in, tenant switches and UserController.LeaveTenant, so there's no need to re-check the user's
            // membership here. Note: There's also no need to check the tenant's IsActive, because deactivating a tenant
            // revokes all of its signed-in sessions immediately (See TenantManagementController.Update).
            var tenantId = userSession.TenantId;

            if (request.RequestedTenantId is Guid requestedTenantId)
            {
                // The user is trying to switch into another tenant.
                var membership = await DbContext.TenantUsers
                    .FirstOrDefaultAsync(tu => tu.UserId == user.Id && tu.TenantId == requestedTenantId, cancellationToken);

                if (membership is null && refreshTicket.Principal.HasFeature(AppFeatures.Management.Tenants_Manage_Global) is false)
                    throw new UnauthorizedException().WithData("Reason", "User doesn't have access to the requested tenant");

                if (await DbContext.Tenants.AnyAsync(t => t.Id == requestedTenantId && t.IsActive, cancellationToken) is false)
                    throw new UnauthorizedException().WithData("Reason", "Inactive (or nonexistent) tenants can't be switched into");

                if (membership is { AcceptedOn: null })
                    membership.AcceptedOn = TimeProvider.GetUtcNow(); // The invitation gets accepted the first time the user switches into the tenant.

                tenantId = requestedTenantId;

                userSession.TenantId = tenantId;

                // The privileged flag is earned against the claims of ONE tenant: MAX_PRIVILEGED_SESSIONS lives on a
                // role, and roles are tenant scoped (See UserClaimsService.GetClaims). It is also sticky, so without
                // clearing it here a session that became privileged under one tenant's claims keeps it under every
                // other tenant's - and since creating a tenant is self service and makes the creator its t-admin with
                // an unlimited claim, that turns "mint a tenant, switch in, switch back" into a way for any account to
                // lift its own session cap, once per device. Cleared so it is re-earned below against the tenant the
                // session is actually moving into (See UpdateUserSessionPrivilegeStatus).
                userSession.Privileged = false;
            }

            if (tenantId is not null)
            {
                userClaimsPrincipalFactory.SetTenantId(tenantId.Value);
            }
            //#endif

            if (elevatedSessionExpiresOn is not null)
            {
                userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.ELEVATED_SESSION, elevatedSessionExpiresOn.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
            }

            // SignInManager stamps amr=mfa on the sign-in that completed the second factor, but this method builds its
            // principal from the factory alone, so without carrying it over the fact would be lost on the first refresh
            // - within five minutes, for a claim that describes how the session was established and never stops being true.
            if (refreshTicket.Principal.HasClaim(AppClaimTypes.AMR, "mfa"))
            {
                userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.AMR, "mfa"));
            }

            userSession.RenewedOn = TimeProvider.GetUtcNow().ToUnixTimeSeconds();
            // Relying on Cloudflare cdn to retrieve address.
            // https://developers.cloudflare.com/rules/transform/managed-transforms/reference/#add-visitor-location-headers
            (userSession.IP, userSession.Address) = (HttpContext.Connection.RemoteIpAddress?.ToString(), $"{Request.Headers["cf-ipcountry"]}, {Request.Headers["cf-ipcity"]}");

            userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.SESSION_ID, currentSessionId.ToString()));

            await UpdateUserSessionPrivilegeStatus(userSession, cancellationToken);

            var newPrincipal = await signInManager.CreateUserPrincipalAsync(user!);

            await DbContext.SaveChangesAsync(cancellationToken);

            return SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
        }
        catch (UnauthorizedException) when (userSession is not null)
        {
            DbContext.UserSessions.Remove(userSession);
            await DbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// For either otp or magic link
    /// </summary>
    [HttpPost, EnableRateLimiting(RateLimitOptionsExtensions.IDENTITY)]
    public async Task SendOtp(IdentityRequestDto request, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindUser(request)
                    ?? await userManager.CreateUserWithDemoRole(request); // Check out SignInModalService for more details

        if (await userConfirmation.IsConfirmedAsync(userManager, user) is false)
        {
            await SendConfirmationToken(user, request.ReturnUrl ?? returnUrl, cancellationToken);
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]).WithData("UserId", user.Id);
        }

        var resendDelay = (TimeProvider.GetUtcNow() - user.OtpRequestedOn) - AppSettings.Identity.OtpTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsException(Localizer[nameof(AppStrings.WaitForOtpRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", resendDelay);

        var (magicLinkToken, url) = await GenerateAutomaticSignInLink(user, request.ReturnUrl ?? returnUrl, originalAuthenticationMethod: "Email");

        var link = new Uri(HttpContext.Request.GetWebAppUrl(), url);

        List<Task> sendMessagesTasks = [];

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendOtp(user, magicLinkToken, link, cancellationToken));
        }

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_Sms,{user.OtpRequestedOn?.ToUniversalTime()}"));
            var message = Localizer[nameof(AppStrings.OtpShortText), token].ToString();
            var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!));
        }

        //#if (signalR == true || notification == true)
        var pushMessage = Localizer[nameof(AppStrings.OtpShortText), await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_Push,{user.OtpRequestedOn?.ToUniversalTime()}"))].ToString();
        //#endif

        //#if (signalR == true)
        var userConnectionIds = await DbContext.UserSessions
            .Where(us => us.NotificationStatus == UserSessionNotificationStatus.Allowed && us.UserId == user.Id && us.SignalRConnectionId != null)
            .Select(us => us.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
        sendMessagesTasks.Add(appHubContext.Clients.Clients(userConnectionIds).SendAsync(SharedAppMessages.SHOW_MESSAGE, pushMessage, null, cancellationToken));
        //#endif

        //#if (notification == true)
        sendMessagesTasks.Add(pushNotificationService.RequestPush(new()
        {
            Message = pushMessage,
            UserRelatedPush = true
        }, customSubscriptionFilter: s => s.UserSession!.UserId == user.Id, cancellationToken: cancellationToken));
        //#endif

        await Task.WhenAll(sendMessagesTasks);
    }

    [HttpPost, EnableRateLimiting(RateLimitOptionsExtensions.IDENTITY)]
    public async Task SendTwoFactorToken(SignInRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);

        var user = await userManager.FindUser(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]).WithData("Identifier", request);

        await SendTwoFactorToken(request, user, cancellationToken);
    }

    private async Task SendTwoFactorToken(SignInRequestDto request, User user, CancellationToken cancellationToken)
    {
        if (user.TwoFactorEnabled is false)
            throw new BadRequestException().WithData("UserId", user.Id);

        bool isOtpSignIn = string.IsNullOrWhiteSpace(request.Otp) is false;

        var (signInResult, firstStepAuthenticationMethod) = isOtpSignIn
            ? await signInManager.OtpSignIn(user, request.Otp!)
            : (await signInManager.PasswordSignInAsync(user!, request.Password!, isPersistent: false, lockoutOnFailure: true), authenticationMethod: "Password");

        if (signInResult.RequiresTwoFactor is false)
            throw new BadRequestException().WithData("UserId", user.Id);

        var resendDelay = (TimeProvider.GetUtcNow() - user.TwoFactorTokenRequestedOn) - AppSettings.Identity.TwoFactorTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsException(Localizer[nameof(AppStrings.WaitForTwoFactorTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", resendDelay);

        user.TwoFactorTokenRequestedOn = TimeProvider.GetUtcNow();
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

        List<Task> sendMessagesTasks = [];

        if (firstStepAuthenticationMethod != "Email" && await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendTwoFactorToken(user, token, cancellationToken));
        }

        var message = Localizer[nameof(AppStrings.TwoFactorTokenShortText), token].ToString();

        if (firstStepAuthenticationMethod != "Sms" && await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!));
        }

        if (firstStepAuthenticationMethod != "Push")
        {
            //#if (signalR == true)
            var userConnectionIds = await DbContext.UserSessions
                .Where(us => us.NotificationStatus == UserSessionNotificationStatus.Allowed && us.UserId == user.Id && us.SignalRConnectionId != null)
                .Select(us => us.SignalRConnectionId!)
                .ToArrayAsync(cancellationToken);
            sendMessagesTasks.Add(appHubContext.Clients.Clients(userConnectionIds).SendAsync(SharedAppMessages.SHOW_MESSAGE, message, null, cancellationToken));
            //#endif
            //#if (notification == true)
            sendMessagesTasks.Add(pushNotificationService.RequestPush(new()
            {
                Message = message,
                UserRelatedPush = true
            }, customSubscriptionFilter: s => s.UserSession!.UserId == user.Id, cancellationToken: cancellationToken));
            //#endif
        }

        await Task.WhenAll(sendMessagesTasks);
    }

    private async Task<(string token, string url)> GenerateAutomaticSignInLink(User user, string? returnUrl, string originalAuthenticationMethod)
    {
        user.OtpRequestedOn = TimeProvider.GetUtcNow();

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_{originalAuthenticationMethod},{user.OtpRequestedOn?.ToUniversalTime()}"));

        var identifier = string.IsNullOrWhiteSpace(user.Email) is false
                            ? $"email={Uri.EscapeDataString(user.Email)}"
                            : $"phoneNumber={Uri.EscapeDataString(user.PhoneNumber!)}";

        var url = $"{PageUrls.SignIn}?otp={Uri.EscapeDataString(token)}&{identifier}&culture={CultureInfo.CurrentUICulture.Name}";

        if (string.IsNullOrWhiteSpace(returnUrl) is false)
        {
            url += $"&return-url={Uri.EscapeDataString(returnUrl)}";
        }

        return (token, url);
    }

    private async Task SendConfirmationToken(User user, string? returnUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email) is false)
        {
            await SendConfirmEmailToken(user, returnUrl, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(user.PhoneNumber) is false)
        {
            await SendConfirmPhoneToken(user, cancellationToken);
        }
    }
}
