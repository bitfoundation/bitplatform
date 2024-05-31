//+:cnd:noEmit
using Microsoft.AspNetCore.Authentication.BearerToken;
using Boilerplate.Server.Services;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Models.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Server.Controllers.Identity;

[ApiController, AllowAnonymous]
[Route("api/[controller]/[action]")]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private SignInManager<User> signInManager = default!;

    [AutoInject] private IUserStore<User> userStore = default!;

    [AutoInject] private IUserConfirmation<User> userConfirmation = default!;

    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    [AutoInject] private SmsService smsService = default!;

    [AutoInject] private EmailService emailService = default!;

    [AutoInject] private ILogger<IdentityController> logger = default!;

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
        var existingUser = await userManager.FindUser(new() { Email = request.Email, PhoneNumber = request.PhoneNumber });
        if (existingUser is not null)
            throw new BadRequestException(Localizer[nameof(AppStrings.DuplicateEmailOrPhoneNumber)]);

        var userToAdd = new User { LockoutEnabled = true };

        await userStore.SetUserNameAsync(userToAdd, request.UserName!, cancellationToken);

        if (string.IsNullOrEmpty(request.Email) is false)
        {
            await ((IUserEmailStore<User>)userStore).SetEmailAsync(userToAdd, request.Email!, cancellationToken);
        }

        if (string.IsNullOrEmpty(request.PhoneNumber) is false)
        {
            await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberAsync(userToAdd, request.PhoneNumber!, cancellationToken);
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

    [HttpPost]
    public async Task SendConfirmEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)]);

        await SendConfirmEmailToken(user, cancellationToken);
    }

    [HttpPost]
    public async Task ConfirmEmail(ConfirmEmailRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsEmailConfirmedAsync(user)) return;

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);

        var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyEmail:{request.Email},Date:{user.EmailTokenRequestedOn}", request.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException();
        }

        var userEmailStore = (IUserEmailStore<User>)userStore;
        await userEmailStore.SetEmailConfirmedAsync(user, true, cancellationToken);
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var updateSecurityStampResult = await userManager.UpdateSecurityStampAsync(user); // invalidates email token
        if (updateSecurityStampResult.Succeeded is false)
            throw new ResourceValidationException(updateSecurityStampResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendConfirmPhoneToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByPhoneNumber(request.PhoneNumber!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.PhoneNumberAlreadyConfirmed)]);

        await SendConfirmPhoneToken(user, cancellationToken);
    }

    [HttpPost]
    public async Task ConfirmPhone(ConfirmPhoneRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByPhoneNumber(request.PhoneNumber!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);

        if (await userManager.IsPhoneNumberConfirmedAsync(user)) return;

        var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyPhoneNumber:{request.PhoneNumber},Date:{user.PhoneNumberTokenRequestedOn}", request.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException();
        }
        await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var updateSecurityStampResult = await userManager.UpdateSecurityStampAsync(user); // invalidates phone token
        if (updateSecurityStampResult.Succeeded is false)
            throw new ResourceValidationException(updateSecurityStampResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<SignInResponseDto>> SignIn(SignInRequestDto request, CancellationToken cancellationToken)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var user = await userManager.FindUser(request) ?? throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        var result = string.IsNullOrEmpty(request.Otp) is false
            ? await signInManager.OtpSignInAsync(user, request.Otp!)
            : await signInManager.PasswordSignInAsync(user!.UserName!, request.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsNotAllowed && await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        if (result.IsLockedOut)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);

        if (result.RequiresTwoFactor)
        {
            if (string.IsNullOrEmpty(request.TwoFactorCode) is false)
            {
                result = await signInManager.TwoFactorAuthenticatorSignInAsync(request.TwoFactorCode, false, false);
            }
            else if (string.IsNullOrEmpty(request.TwoFactorRecoveryCode) is false)
            {
                result = await signInManager.TwoFactorRecoveryCodeSignInAsync(request.TwoFactorRecoveryCode);
            }
            else if (string.IsNullOrEmpty(request.TwoFactorToken) is false)
            {
                result = await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultPhoneProvider, request.TwoFactorToken, false, false);
            }
            else
            {
                return new SignInResponseDto { RequiresTwoFactor = true };
            }
        }

        if (result.Succeeded is false)
            throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        if (string.IsNullOrEmpty(request.Otp) is false)
        {
            var updateSecurityStampResult = await userManager.UpdateSecurityStampAsync(user); // invalidates the OTP.
            if (updateSecurityStampResult.Succeeded is false)
                throw new ResourceValidationException(updateSecurityStampResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        return Empty;
    }

    [HttpPost]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshRequestDto request)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(request.RefreshToken);

        if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc ||
                await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not User user)
        {
            return Challenge();
        }

        var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);

        return SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    [HttpPost]
    public async Task SendResetPasswordToken(SendResetPasswordTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindUser(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        var resendDelay = (DateTimeOffset.Now - user.ResetPasswordTokenRequestedOn) - AppSettings.IdentitySettings.ResetPasswordTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForResetPasswordTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.ResetPasswordTokenRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"ResetPassword,Date:{user.ResetPasswordTokenRequestedOn}");
        var isEmail = string.IsNullOrEmpty(request.Email) is false;
        var qs = $"{(isEmail ? "email" : "phoneNumber")}={Uri.EscapeDataString(isEmail ? request.Email! : request.PhoneNumber!)}";
        var url = $"reset-password?token={Uri.EscapeDataString(token)}&{qs}";
        var link = new Uri(HttpContext.Request.GetBaseUrl(), url);

        async Task SendEmail()
        {
            if (await userManager.IsEmailConfirmedAsync(user) is false) return;

            await emailService.SendResetPasswordToken(user, token, link, cancellationToken);
        }

        async Task SendSms()
        {
            if (await userManager.IsPhoneNumberConfirmedAsync(user) is false) return;

            await smsService.SendSms(Localizer[nameof(AppStrings.ResetPasswordTokenSmsText), token], user.PhoneNumber!, cancellationToken);
        }

        await Task.WhenAll(SendEmail(), SendSms());
    }

    /// <summary>
    /// For either otp or magic link
    /// </summary>
    [HttpPost]
    public async Task SendOtp(IdentityRequestDto request, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindUser(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        var resendDelay = (DateTimeOffset.Now - user.OtpRequestedOn) - AppSettings.IdentitySettings.OtpRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForOtpRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.OtpRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var (token, pageUrl) = await GenerateOtpTokenData(user, returnUrl);

        var link = new Uri(HttpContext.Request.GetBaseUrl(), pageUrl);

        async Task SendEmail()
        {
            if (await userManager.IsEmailConfirmedAsync(user) is false) return;

            await emailService.SendOtp(user, token, link, cancellationToken);
        }

        async Task SendSms()
        {
            if (await userManager.IsPhoneNumberConfirmedAsync(user) is false) return;

            await smsService.SendSms(Localizer[nameof(AppStrings.OtpSmsText), token], user.PhoneNumber!, cancellationToken);
        }

        await Task.WhenAll(SendEmail(), SendSms());
    }

    [HttpPost]
    public async Task ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindUser(request) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);

        bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, $"ResetPassword,Date:{user.ResetPasswordTokenRequestedOn}", request.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException();
        }

        var result = await userManager.ResetPasswordAsync(user!, await userManager.GeneratePasswordResetTokenAsync(user!), request.Password!);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var updateSecurityStampResult = await userManager.UpdateSecurityStampAsync(user); // invalidates reset password token
        if (updateSecurityStampResult.Succeeded is false)
            throw new ResourceValidationException(updateSecurityStampResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendTwoFactorToken(IdentityRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindUser(request) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        var resendDelay = (DateTimeOffset.Now - user.TwoFactorTokenRequestedOn) - AppSettings.IdentitySettings.TwoFactorTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForTwoFactorTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.TwoFactorTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

        async Task SendEmail()
        {
            if (await userManager.IsEmailConfirmedAsync(user))
            {
                await emailService.SendTwoFactorToken(user, token, cancellationToken);
            }
        }

        async Task SendSms()
        {
            if (await userManager.IsPhoneNumberConfirmedAsync(user))
            {
                await smsService.SendSms(Localizer[nameof(AppStrings.TwoFactorTokenSmsText), token], user.PhoneNumber!, cancellationToken);
            }
        }

        await Task.WhenAll(SendEmail(), SendSms());
    }

    [HttpGet]
    public async Task<string> GetSocialSignInUri(string provider, string? returnUrl = null, int? localHttpPort = null)
    {
        var uri = Url.Action(nameof(SocialSignIn), new { provider, returnUrl, localHttpPort })!;
        return new Uri(Request.GetBaseUrl(), uri).ToString();
    }

    [HttpGet]
    public async Task<ActionResult> SocialSignIn(string provider, string? returnUrl = null, int? localHttpPort = null)
    {
        var redirectUrl = Url.Action(nameof(SocialSignInCallback), "Identity", new { returnUrl, localHttpPort });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    [HttpGet]
    public async Task<ActionResult> SocialSignInCallback(string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default)
    {
        string? pageUrl;

        var info = await signInManager.GetExternalLoginInfoAsync();

        if (info is null)
            throw new BadRequestException();

        try
        {
            var email = info.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var phoneNumber = info.Principal.Claims.FirstOrDefault(c => c.Type is ClaimTypes.HomePhone or ClaimTypes.MobilePhone or ClaimTypes.OtherPhone)?.Value;

            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phoneNumber))
                throw new InvalidOperationException(); // The app requires users to have at least one communication channel: phone or email.

            var user = await userManager.FindUser(new() { Email = email, PhoneNumber = phoneNumber });

            if (user is null)
            {
                user = new User { LockoutEnabled = true };

                await userStore.SetUserNameAsync(user, Guid.NewGuid().ToString(), cancellationToken);

                if (string.IsNullOrEmpty(email) is false)
                {
                    await ((IUserEmailStore<User>)userStore).SetEmailAsync(user, email, cancellationToken);
                }

                if (string.IsNullOrEmpty(phoneNumber) is false)
                {
                    await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberAsync(user, phoneNumber!, cancellationToken);
                }

                var result = await userManager.CreateAsync(user, Guid.NewGuid().ToString("N") /* Users can reset their password later. */);

                if (result.Succeeded is false)
                {
                    throw new BadRequestException(string.Join(", ", result.Errors.Select(e => new LocalizedString(e.Code, e.Description))));
                }

                await userManager.AddLoginAsync(user, info);
            }

            if (string.IsNullOrEmpty(email) is false && email == user.Email && await userManager.IsEmailConfirmedAsync(user) is false)
            {
                await ((IUserEmailStore<User>)userStore).SetEmailConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }

            if (string.IsNullOrEmpty(phoneNumber) is false && phoneNumber == user.PhoneNumber && await userManager.IsPhoneNumberConfirmedAsync(user) is false)
            {
                await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }

            (_, pageUrl) = await GenerateOtpTokenData(user, returnUrl); // Sign in with a magic link, and 2FA will be prompted if already enabled.
        }
        catch (Exception exp)
        {
            LogSocialSignInCallbackFailed(logger, exp, info.ProviderKey);
            pageUrl = $"sign-in?error={Uri.EscapeDataString(exp is KnownException ? Localizer[exp.Message] : Localizer[nameof(AppStrings.UnknownException)])}";
        }

        if (localHttpPort is null) return LocalRedirect($"~/{pageUrl}");

        return Redirect(new Uri(new Uri($"http://localhost:{localHttpPort}/"), pageUrl).ToString());
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to perform social sign in for {provider}")]
    private static partial void LogSocialSignInCallbackFailed(ILogger logger, Exception exp, string provider);

    private async Task<(string token, string pageUrl)> GenerateOtpTokenData(User user, string? returnUrl)
    {
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"Otp,Date:{user.OtpRequestedOn}");
        var isEmail = string.IsNullOrEmpty(user.Email) is false;
        var qs = $"{(isEmail ? "email" : "phoneNumber")}={Uri.EscapeDataString(isEmail ? user.Email! : user.PhoneNumber!)}";
        if (string.IsNullOrEmpty(returnUrl) is false)
            qs += $"&return-url={Uri.EscapeDataString(returnUrl)}";
        var pageUrl = $"sign-in?otp={Uri.EscapeDataString(token)}&{qs}";

        return (token, pageUrl);
    }

    private async Task SendConfirmEmailToken(User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.EmailTokenRequestedOn) - AppSettings.IdentitySettings.EmailTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForEmailTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.EmailTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var email = user.Email!;
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyEmail:{email},Date:{user.EmailTokenRequestedOn}");
        var link = new Uri(HttpContext.Request.GetBaseUrl(), $"confirm?email={Uri.EscapeDataString(email)}&emailToken={Uri.EscapeDataString(token)}");

        await emailService.SendEmailToken(user, email, token, link, cancellationToken);
    }

    private async Task SendConfirmPhoneToken(User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.PhoneNumberTokenRequestedOn) - AppSettings.IdentitySettings.PhoneNumberTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForPhoneNumberTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.PhoneNumberTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var phoneNumber = user.PhoneNumber!;
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyPhoneNumber:{phoneNumber},Date:{user.PhoneNumberTokenRequestedOn}");
        var link = new Uri(HttpContext.Request.GetBaseUrl(), $"confirm?phoneNumber={Uri.EscapeDataString(phoneNumber!)}&phoneToken={Uri.EscapeDataString(token)}");

        await smsService.SendSms(Localizer[nameof(AppStrings.ConfirmPhoneTokenSmsText), token], phoneNumber, cancellationToken);
    }
}
