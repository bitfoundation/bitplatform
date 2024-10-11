//+:cnd:noEmit
using Humanizer;
using Microsoft.AspNetCore.Authentication.BearerToken;
//#if (signalr == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.Hubs;
//#endif
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, AllowAnonymous]
[Route("api/[controller]/[action]")]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private IUserStore<User> userStore = default!;
    [AutoInject] private IUserEmailStore<User> userEmailStore = default!;
    [AutoInject] private IUserPhoneNumberStore<User> userPhoneNumberStore = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private SignInManager<User> signInManager = default!;
    [AutoInject] private ILogger<IdentityController> logger = default!;
    [AutoInject] private IUserConfirmation<User> userConfirmation = default!;
    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;
    [AutoInject] private AppUserClaimsPrincipalFactory userClaimsPrincipalFactory = default!;
    //#if (signalr == true)
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
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var user = await userManager.FindUserAsync(request) ?? throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        var userSession = CreateUserSession(request.DeviceInfo);

        bool isOtpSignIn = string.IsNullOrEmpty(request.Otp) is false;

        if (isOtpSignIn)
        {
            var expired = (DateTimeOffset.Now - user.OtpRequestedOn) > AppSettings.Identity.OtpTokenLifetime;

            if (expired)
                throw new BadRequestException(nameof(AppStrings.ExpiredToken));
        }

        var result = isOtpSignIn
            ? await signInManager.OtpSignInAsync(user, request.Otp!)
            : await signInManager.PasswordSignInAsync(user!.UserName!, request.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsNotAllowed && await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        if (result.IsLockedOut)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        if (result.RequiresTwoFactor)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode) is false)
            {
                result = await CheckTwoFactorCode(request.TwoFactorCode);
            }
            else
            {
                await Response.WriteAsJsonAsync(new SignInResponseDto { RequiresTwoFactor = true }, cancellationToken);
                return;
            }
        }

        if (result.Succeeded is false)
            throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        if (string.IsNullOrEmpty(request.Otp) is false)
        {
            await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
            user.OtpRequestedOn = null; // invalidates the OTP
            var updateResult = await userManager.UpdateAsync(user);
            if (updateResult.Succeeded is false)
                throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        user.Sessions.Add(userSession);
        var addUserSessionResult = await userManager.UpdateAsync(user);
        if (addUserSessionResult.Succeeded is false)
            throw new ResourceValidationException(addUserSessionResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
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
    /// Creates a user session and adds its ID to the access and refresh tokens, but only if the sign-in is successful.
    /// </summary>
    private UserSession CreateUserSession(string? device)
    {
        var userSession = new UserSession
        {
            SessionUniqueId = Guid.NewGuid(),
            // Relying on Cloudflare cdn to retrieve address.
            // https://developers.cloudflare.com/rules/transform/managed-transforms/reference/#add-visitor-location-headers
            Address = $"{Request.Headers["cf-ipcountry"]}, {Request.Headers["cf-ipcity"]}",
            Device = device ?? Localizer[nameof(AppStrings.UnknwonDevice)],
            IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
            StartedOn = DateTimeOffset.UtcNow
        };

        userClaimsPrincipalFactory.SessionClaims.Add(new("session-id", userSession.SessionUniqueId.ToString()));

        return userSession;
    }

    [HttpPost]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshRequestDto request)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(request.RefreshToken);

        if (refreshTicket?.Principal.IsAuthenticated() is false)
            throw new UnauthorizedException();

        string? userId = null; User? user = null; UserSession? userSession = null;

        userId = refreshTicket?.Principal?.GetUserId().ToString();
        if (string.IsNullOrEmpty(userId) is false)
        {
            user = await userManager.FindByIdAsync(userId) ?? throw new UnauthorizedException();
        }
        if (Guid.TryParse(refreshTicket?.Principal?.FindFirstValue("session-id"), out var currentSessionId))
        {
            userSession = user!.Sessions.Find(s => s.SessionUniqueId == currentSessionId);
        }

        bool isExpiredSession = refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc ||
            DateTimeOffset.UtcNow >= expiresUtc ||
            await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not User _ ||
            userSession is null;

        if (userSession is not null)
        {
            if (isExpiredSession)
            {
                user!.Sessions.Remove(userSession);
            }
            else
            {
                userSession!.RenewedOn = DateTimeOffset.UtcNow;
            }

            try
            {
                await userManager.UpdateAsync(user!);
            }
            catch (ConflictException) { /* When access_token gets expired and user navigates to the page that sends multiple requests in parallel, multiple concurrent refresh token api call happens and this will results into concurrency exception during updating session's renewed on. */ }
        }

        if (isExpiredSession)
        {
            // Return 401 if refresh token is either invalid or expired.
            throw new UnauthorizedException();
        }

        userClaimsPrincipalFactory.SessionClaims.Add(new("session-id", currentSessionId.ToString()));

        var newPrincipal = await signInManager.CreateUserPrincipalAsync(user!);

        return SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    /// <summary>
    /// For either otp or magic link
    /// </summary>
    [HttpPost]
    public async Task SendOtp(IdentityRequestDto request, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindUserAsync(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        var resendDelay = (DateTimeOffset.Now - user.OtpRequestedOn) - AppSettings.Identity.OtpTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForOtpRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        var (token, url) = await GenerateOtpTokenData(user, returnUrl);

        var link = new Uri(HttpContext.Request.GetWebClientUrl(), url);

        List<Task> sendMessagesTasks = [];

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendOtp(user, token, link, cancellationToken));
        }

        var message = Localizer[nameof(AppStrings.OtpShortText), token];

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            sendMessagesTasks.Add(smsService.SendSms(message, user.PhoneNumber!, cancellationToken));
        }

        //#if (signalr == true)
        sendMessagesTasks.Add(appHubContext.Clients.User(user.Id.ToString()).SendAsync(method: "DisplayMessage", message, cancellationToken));
        //#endif

        //#if (notification == true)
        sendMessagesTasks.Add(pushNotificationService.RequestPush(message: message, customDeviceFilter: d => d.UserId == user.Id, cancellationToken: cancellationToken));
        //#endif

        await Task.WhenAll(sendMessagesTasks);
    }

    [HttpPost]
    public async Task SendTwoFactorToken(IdentityRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindUserAsync(request) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        var resendDelay = (DateTimeOffset.Now - user.TwoFactorTokenRequestedOn) - AppSettings.Identity.TwoFactorTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForTwoFactorTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        user.TwoFactorTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

        List<Task> sendMessagesTasks = [];

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendTwoFactorToken(user, token, cancellationToken));
        }

        var message = Localizer[nameof(AppStrings.TwoFactorTokenShortText), token];

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            sendMessagesTasks.Add(smsService.SendSms(message, user.PhoneNumber!, cancellationToken));
        }

        //#if (signalr == true)
        sendMessagesTasks.Add(appHubContext.Clients.User(user.Id.ToString()).SendAsync(method: "DisplayMessage", message, cancellationToken));
        //#endif

        //#if (notification == true)
        sendMessagesTasks.Add(pushNotificationService.RequestPush(message: message, customDeviceFilter: d => d.UserId == user.Id, cancellationToken: cancellationToken));
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to perform {loginProvider} social sign in for {principal}")]
    private static partial void LogSocialSignInCallbackFailed(ILogger logger, Exception exp, string loginProvider, string principal);

    private async Task<(string token, string url)> GenerateOtpTokenData(User user, string? returnUrl)
    {
        user.OtpRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp,{user.OtpRequestedOn?.ToUniversalTime()}"));

        var qs = $"userName={Uri.EscapeDataString(user.UserName!)}";

        if (string.IsNullOrEmpty(returnUrl) is false)
        {
            qs += $"&return-url={Uri.EscapeDataString(returnUrl)}";
        }

        var url = $"{Urls.SignInPage}?otp={Uri.EscapeDataString(token)}&{qs}&culture={CultureInfo.CurrentUICulture.Name}";

        return (token, url);
    }
}
