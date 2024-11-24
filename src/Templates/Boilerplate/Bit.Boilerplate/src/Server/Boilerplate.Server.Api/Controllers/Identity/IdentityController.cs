﻿//+:cnd:noEmit
using Humanizer;
using Microsoft.AspNetCore.Authentication.BearerToken;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Server.Api.Services.Identity;
using System.Threading;

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, AllowAnonymous]
[Route("api/[controller]/[action]")]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private IUserStore<User> userStore = default!;
    [AutoInject] private IUserEmailStore<User> userEmailStore = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private SignInManager<User> signInManager = default!;
    [AutoInject] private ILogger<IdentityController> logger = default!;
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
    [AutoInject] private GoogleRecaptchaHttpClient googleRecaptchaHttpClient = default!;
    //#endif

    /// <summary>
    /// By leveraging summary tags in your controller's actions and DTO properties you can make your codes much easier to maintain.
    /// These comments will also be used in swagger docs and ui.
    /// </summary>
    [HttpPost]
    public async Task SignUp(SignUpRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        //#if (captcha == "reCaptcha")
        if (await googleRecaptchaHttpClient.Verify(request.GoogleRecaptchaResponse, cancellationToken) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.InvalidGoogleRecaptchaResponse)]);
        //#endif

        // Attempt to locate an existing user using either their email address or phone number. The enforcement of a unique username policy is integral to the aspnetcore identity framework.
        var existingUser = await userManager.FindUserAsync(new() { Email = request.Email, PhoneNumber = request.PhoneNumber });
        if (existingUser is not null)
            throw new BadRequestException(Localizer[nameof(AppStrings.DuplicateEmailOrPhoneNumber)]);

        var userToAdd = new User { LockoutEnabled = true };

        await userStore.SetUserNameAsync(userToAdd, request.UserName!, cancellationToken);

        if (string.IsNullOrEmpty(request.Email) is false)
        {
            await userEmailStore.SetEmailAsync(userToAdd, request.Email!, cancellationToken);
        }

        if (string.IsNullOrEmpty(request.PhoneNumber) is false)
        {
            await userPhoneNumberStore.SetPhoneNumberAsync(userToAdd, request.PhoneNumber!, cancellationToken);
        }

        var result = await userManager.CreateAsync(userToAdd, request.Password!);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        if (string.IsNullOrEmpty(userToAdd.Email) is false)
        {
            await SendConfirmEmailToken(userToAdd, cancellationToken);
        }

        if (string.IsNullOrEmpty(userToAdd.PhoneNumber) is false)
        {
            await SendConfirmPhoneToken(userToAdd, cancellationToken);
        }
    }

    [HttpPost, Produces<SignInResponseDto>()]
    public async Task SignIn(SignInRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var user = await userManager.FindUserAsync(request) ?? throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        var userSession = await CreateUserSession(user.Id, request.DeviceInfo, cancellationToken);
        userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.SESSION_ID, userSession.Id.ToString()));
        userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.PRIVILEGED_SESSION, "true")); // This only applies to the current, short-lived access token.
        if (userSession.Licensed)
        {
            userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.LICENSED_SESSION, "true"));
        }

        bool isOtpSignIn = string.IsNullOrEmpty(request.Otp) is false;

        var (signInResult, firstStepAuthenticationMethod) = isOtpSignIn
            ? await signInManager.OtpSignInAsync(user, request.Otp!)
            : (await signInManager.PasswordSignInAsync(user!.UserName!, request.Password!, isPersistent: false, lockoutOnFailure: true), authenticationMethod: "Password");

        if (signInResult.IsNotAllowed && await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        if (signInResult.IsLockedOut)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        if (signInResult.RequiresTwoFactor)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode) is false)
            {
                signInResult = await CheckTwoFactorCode(request.TwoFactorCode);
            }
            else
            {
                await Response.WriteAsJsonAsync(new SignInResponseDto { RequiresTwoFactor = true }, cancellationToken);
                return;
            }
        }

        if (signInResult.Succeeded is false)
            throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        if (string.IsNullOrEmpty(request.Otp) is false)
        {
            await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
            user.OtpRequestedOn = null; // invalidates the OTP
            var updateResult = await userManager.UpdateAsync(user);
            if (updateResult.Succeeded is false)
                throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        DbContext.UserSessions.Add(userSession);
        user.TwoFactorTokenRequestedOn = null;
        var addUserSessionResult = await userManager.UpdateAsync(user);
        if (addUserSessionResult.Succeeded is false)
            throw new ResourceValidationException(addUserSessionResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Microsoft.AspNetCore.Identity.SignInResult> CheckTwoFactorCode(string code)
    {
        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded is false)
        {
            result = await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultPhoneProvider, code, false, false);
        }

        if (result.Succeeded is false)
        {
            result = await signInManager.TwoFactorAuthenticatorSignInAsync(code, false, false);
        }

        return result;
    }

    /// <summary>
    /// Creates a user session and adds its ID to the access and refresh tokens, but only if the sign-in is successful <see cref="AppUserClaimsPrincipalFactory.SessionClaims"/>
    /// </summary>
    private async Task<UserSession> CreateUserSession(Guid userId, string? deviceInfo, CancellationToken cancellationToken)
    {
        var userSession = new UserSession
        {
            Id = Guid.NewGuid(),
            DeviceInfo = deviceInfo,
            UserId = userId,
            StartedOn = DateTimeOffset.UtcNow,
            IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
            // Relying on Cloudflare cdn to retrieve address.
            // https://developers.cloudflare.com/rules/transform/managed-transforms/reference/#add-visitor-location-headers
            Address = $"{Request.Headers["cf-ipcountry"]}, {Request.Headers["cf-ipcity"]}",
        };

        userSession.Licensed = await IsUserSessionLicensed(userSession, cancellationToken);

        return userSession;
    }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.LICENSED_ACCESS"/>
    /// </summary>
    private async Task<bool> IsUserSessionLicensed(UserSession userSession, CancellationToken cancellationToken)
    {
        return userSession.Licensed is true ||
            await DbContext.UserSessions.CountAsync(us => us.Licensed == true, cancellationToken) < 3;
    }

    [HttpPost]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshRequestDto request, CancellationToken cancellationToken)
    {
        UserSession? userSession = null;
        User? user = null;

        try
        {
            var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(request.RefreshToken);

            if (refreshTicket?.Principal.IsAuthenticated() is false
                || (refreshTicket!.Properties.ExpiresUtc ?? DateTimeOffset.MinValue) < DateTimeOffset.UtcNow)
                throw new UnauthorizedException();

            var userId = refreshTicket!.Principal.GetUserId().ToString() ?? throw new InvalidOperationException("User id could not be found");
            var currentSessionId = Guid.Parse(refreshTicket.Principal.FindFirstValue(AppClaimTypes.SESSION_ID) ?? throw new InvalidOperationException("session id could not be found"));

            user = await userManager.FindByIdAsync(userId) ?? throw new UnauthorizedException(); // User might have been deleted.
            userSession = await DbContext.UserSessions
                .FirstOrDefaultAsync(us => us.Id == currentSessionId) ?? throw new UnauthorizedException(); // User session might have been deleted.

            if (await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not User _)
                throw new UnauthorizedException();

            userSession.RenewedOn = DateTimeOffset.UtcNow;

            userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.SESSION_ID, currentSessionId.ToString()));

            userSession.Licensed = await IsUserSessionLicensed(userSession, cancellationToken);
            if (userSession.Licensed is true)
            {
                userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.LICENSED_SESSION, "true"));
            }

            if (string.IsNullOrEmpty(request.PrivilegedAccessToken) is false)
            {
                var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"PrivilegedAccess:{userSession.Id},{user.PrivilegedAccessTokenRequestedOn?.ToUniversalTime()}"), request.PrivilegedAccessToken);
                if (tokenIsValid is false)
                {
                    await userManager.AccessFailedAsync(user);
                    throw new UnauthorizedException(nameof(AppStrings.InvalidToken));
                }
                else
                {
                    user.PrivilegedAccessTokenRequestedOn = null; // invalidates token
                    await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
                    userClaimsPrincipalFactory.SessionClaims.Add(new(AppClaimTypes.PRIVILEGED_SESSION, "true"));
                }
            }

            var newPrincipal = await signInManager.CreateUserPrincipalAsync(user!);

            return SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
        }
        catch when (userSession is not null)
        {
            //#if (notification == true)
            // In order to have the code that works with databases without cascade delete support, we're loading subscriptions, so ef core will set their UserSessionId to null
            await DbContext.Entry(userSession).Reference(us => us.PushNotificationSubscription).LoadAsync(cancellationToken);
            //#endif
            DbContext.UserSessions.Remove(userSession);
            throw;
        }
        finally
        {
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// For either otp or magic link
    /// </summary>
    [HttpPost]
    public async Task SendOtp(IdentityRequestDto request, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindUserAsync(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        var resendDelay = (DateTimeOffset.Now - user.OtpRequestedOn) - AppSettings.Identity.OtpTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForOtpRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        var (magicLinkToken, url) = await GenerateAutomaticSignInLink(user, returnUrl, originalAuthenticationMethod: "Email");

        var link = new Uri(HttpContext.Request.GetWebClientUrl(), url);

        List<Task> sendMessagesTasks = [];

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendOtp(user, magicLinkToken, link, cancellationToken));
        }

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            var smsMessage = Localizer[nameof(AppStrings.OtpShortText), await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_Sms,{user.OtpRequestedOn?.ToUniversalTime()}"))].ToString();
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!, cancellationToken));
        }

        //#if (signalR == true || notification == true)
        var pushMessage = Localizer[nameof(AppStrings.OtpShortText), await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_Push,{user.OtpRequestedOn?.ToUniversalTime()}"))].ToString();
        //#endif

        //#if (signalR == true)
        // Checkout AppHubConnectionHandler's comments for more info.
        sendMessagesTasks.Add(appHubContext.Clients.User(user.Id.ToString()).SendAsync(SignalREvents.SHOW_MESSAGE, pushMessage, cancellationToken));
        //#endif

        //#if (notification == true)
        sendMessagesTasks.Add(pushNotificationService.RequestPush(message: pushMessage, userRelatedPush: true, customSubscriptionFilter: s => s.UserSession!.UserId == user.Id, cancellationToken: cancellationToken));
        //#endif

        await Task.WhenAll(sendMessagesTasks);
    }

    [HttpPost]
    public async Task SendTwoFactorToken(SignInRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindUserAsync(request) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (user.TwoFactorEnabled is false)
            throw new BadRequestException();

        bool isOtpSignIn = string.IsNullOrEmpty(request.Otp) is false;

        var (signInResult, firstStepAuthenticationMethod) = isOtpSignIn
            ? await signInManager.OtpSignInAsync(user, request.Otp!)
            : (await signInManager.PasswordSignInAsync(user!.UserName!, request.Password!, isPersistent: false, lockoutOnFailure: true), authenticationMethod: "Password");

        if (signInResult.RequiresTwoFactor is false)
            throw new BadRequestException();

        var resendDelay = (DateTimeOffset.Now - user.TwoFactorTokenRequestedOn) - AppSettings.Identity.TwoFactorTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForTwoFactorTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        user.TwoFactorTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

        List<Task> sendMessagesTasks = [];

        if (firstStepAuthenticationMethod != "Email" && await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendTwoFactorToken(user, token, cancellationToken));
        }

        var message = Localizer[nameof(AppStrings.TwoFactorTokenShortText), token].ToString();

        if (firstStepAuthenticationMethod != "Sms" && await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            sendMessagesTasks.Add(phoneService.SendSms(message, user.PhoneNumber!, cancellationToken));
        }

        //#if (signalR == true)
        if (firstStepAuthenticationMethod != "SignalR")
        {
            // Checkout AppHubConnectionHandler's comments for more info.
            sendMessagesTasks.Add(appHubContext.Clients.User(user.Id.ToString()).SendAsync(SignalREvents.SHOW_MESSAGE, message, cancellationToken));
        }
        //#endif

        //#if (notification == true)
        if (firstStepAuthenticationMethod != "Push")
        {
            sendMessagesTasks.Add(pushNotificationService.RequestPush(message: message, userRelatedPush: true, customSubscriptionFilter: s => s.UserSession!.UserId == user.Id, cancellationToken: cancellationToken));
        }
        //#endif

        await Task.WhenAll(sendMessagesTasks);
    }

    [HttpGet]
    public async Task<ActionResult> SocialSignedIn()
    {
        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                    (await htmlRenderer.RenderComponentAsync<SocialSignedInPage>()).ToHtmlString());

        return Content(html, "text/html");
    }

    private async Task<(string token, string url)> GenerateAutomaticSignInLink(User user, string? returnUrl, string originalAuthenticationMethod)
    {
        user.OtpRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_{originalAuthenticationMethod},{user.OtpRequestedOn?.ToUniversalTime()}"));

        var qs = $"userName={Uri.EscapeDataString(user.UserName!)}";

        if (string.IsNullOrEmpty(returnUrl) is false)
        {
            qs += $"&return-url={Uri.EscapeDataString(returnUrl)}";
        }

        var url = $"{Urls.SignInPage}?otp={Uri.EscapeDataString(token)}&{qs}&culture={CultureInfo.CurrentUICulture.Name}";

        return (token, url);
    }
}
