//+:cnd:noEmit
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Authentication.BearerToken;
using FluentEmail.Core;
using Boilerplate.Server.Services;
using Boilerplate.Server.Resources;
using Boilerplate.Server.Components;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Models.Emailing;
using Boilerplate.Server.Models.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Server.Controllers.Identity;

[ApiController, AllowAnonymous]
[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private SignInManager<User> signInManager = default!;

    [AutoInject] private IUserStore<User> userStore = default!;

    [AutoInject] private IFluentEmail fluentEmail = default!;

    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    [AutoInject] private HtmlRenderer htmlRenderer = default!;

    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    [AutoInject] private SmsService smsService = default!;


    //#if (captcha == "reCaptcha")
    [AutoInject] private GoogleRecaptchaHttpClient googleRecaptchaHttpClient = default!;
    //#endif

    /// <summary>
    /// By leveraging summary tags in your controller's actions and DTO properties you can make your codes much easier to maintain.
    /// These comments will also be used in swagger docs and ui.
    /// </summary>
    [HttpPost]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        //#if (captcha == "reCaptcha")
        if (await googleRecaptchaHttpClient.Verify(signUpRequest.GoogleRecaptchaResponse, cancellationToken) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.InvalidGoogleRecaptchaResponse)]);
        //#endif

        // Attempt to locate an existing user using either their email address or phone number. The enforcement of a unique username policy is integral to the aspnetcore identity framework.
        var existingUser = await userManager.FindUser(new() { Email = signUpRequest.Email, PhoneNumber = signUpRequest.PhoneNumber });
        if (existingUser is not null)
            throw new BadRequestException(Localizer[nameof(AppStrings.DuplicateEmailOrPhoneNumber)]);

        var userToAdd = new User { LockoutEnabled = true };

        await userStore.SetUserNameAsync(userToAdd, signUpRequest.UserName!, cancellationToken);

        if (string.IsNullOrEmpty(signUpRequest.Email) is false)
        {
            await ((IUserEmailStore<User>)userStore).SetEmailAsync(userToAdd, signUpRequest.Email!, cancellationToken);
        }

        if (string.IsNullOrEmpty(signUpRequest.PhoneNumber) is false)
        {
            await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberAsync(userToAdd, signUpRequest.PhoneNumber!, cancellationToken);
        }

        var result = await userManager.CreateAsync(userToAdd, signUpRequest.Password!);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        if (string.IsNullOrEmpty(userToAdd.Email) is false)
        {
            await SendConfirmEmailToken(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
        }

        if (string.IsNullOrEmpty(userToAdd.PhoneNumber) is false)
        {
            await SendConfirmPhoneToken(new() { PhoneNumber = userToAdd.PhoneNumber }, userToAdd, cancellationToken);
        }
    }

    [HttpPost]
    public async Task SendConfirmEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)]);

        await SendConfirmEmailToken(request, user, cancellationToken);
    }

    [HttpPost]
    public async Task ConfirmEmail(ConfirmEmailRequestDto body, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(body.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (user.EmailConfirmed) return;

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);

        var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyEmail:{body.Email},Date:{user.EmailTokenRequestedOn}", body.Token!);

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
    }

    [HttpPost]
    public async Task SendConfirmPhoneToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByPhoneNumber(request.PhoneNumber!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.PhoneNumberAlreadyConfirmed)]);

        await SendConfirmPhoneToken(request, user, cancellationToken);
    }

    [HttpPost]
    public async Task ConfirmPhone(ConfirmPhoneRequestDto body, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByPhoneNumber(body.PhoneNumber!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);

        if (user.PhoneNumberConfirmed) return;

        var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyPhoneNumber:{body.PhoneNumber},Date:{user.PhoneNumberTokenRequestedOn}", body.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException();
        }
        await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<SignInResponseDto>> SignIn(SignInRequestDto signInRequest, CancellationToken cancellationToken)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var user = await userManager.FindUser(signInRequest) ?? throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        Microsoft.AspNetCore.Identity.SignInResult? result = null;

        result = string.IsNullOrEmpty(signInRequest.Password)
            ? await signInManager.OtpSignInAsync(user, signInRequest.Otp!)
            : await signInManager.PasswordSignInAsync(user!.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);
        }

        if (result.RequiresTwoFactor)
        {
            if (string.IsNullOrEmpty(signInRequest.TwoFactorCode) is false)
            {
                result = await signInManager.TwoFactorAuthenticatorSignInAsync(signInRequest.TwoFactorCode, false, false);
            }
            else if (string.IsNullOrEmpty(signInRequest.TwoFactorRecoveryCode) is false)
            {
                result = await signInManager.TwoFactorRecoveryCodeSignInAsync(signInRequest.TwoFactorRecoveryCode);
            }
            else if (string.IsNullOrEmpty(signInRequest.TwoFactorToken) is false)
            {
                result = await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultPhoneProvider, signInRequest.TwoFactorToken, false, false);
            }
            else
            {
                return new SignInResponseDto { RequiresTwoFactor = true };
            }
        }

        if (result.Succeeded is false)
            throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUserCredentials)]);

        return Empty;
    }

    [HttpPost]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshRequestDto refreshRequest)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

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

            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(ResetPasswordTokenTemplate.Model)] = new ResetPasswordTokenTemplateModel
                {
                    Token = token,
                    Link = link,
                    DisplayName = user.DisplayName!,
                },
                [nameof(HttpContext)] = HttpContext
            });

            var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var renderedComponent = await htmlRenderer.RenderComponentAsync<ResetPasswordTokenTemplate>(parameters);

                return renderedComponent.ToHtmlString();
            });

            var emailResult = await fluentEmail.To(user.Email, user.DisplayName)
                                               .Subject(emailLocalizer[EmailStrings.ResetPasswordEmailSubject])
                                               .Body(body, isHtml: true)
                                               .SendAsync(cancellationToken);

            if (emailResult.Successful is false)
                throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => Localizer[err]).ToArray());
        }

        async Task SendSms()
        {
            if (await userManager.IsPhoneNumberConfirmedAsync(user) is false) return;

            await smsService.SendSms(Localizer[nameof(AppStrings.YourResetPasswordToken), token], user.PhoneNumber!, cancellationToken);
        }

        await Task.WhenAll(SendEmail(), SendSms());
    }

    /// <summary>
    /// For either otp or magic link
    /// </summary>
    [HttpPost]
    public async Task SendOtp(SendOtpRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindUser(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        var resendDelay = (DateTimeOffset.Now - user.OtpRequestedOn) - AppSettings.IdentitySettings.OtpRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForOtpRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.OtpRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"Otp,Date:{user.OtpRequestedOn}");
        var isEmail = string.IsNullOrEmpty(request.Email) is false;
        var qs = $"{(isEmail ? "email" : "phoneNumber")}={Uri.EscapeDataString(isEmail ? request.Email! : request.PhoneNumber!)}";
        var url = $"otp?token={Uri.EscapeDataString(token)}&{qs}";
        var link = new Uri(HttpContext.Request.GetBaseUrl(), url);

        async Task SendEmail()
        {
            if (await userManager.IsEmailConfirmedAsync(user) is false) return;

            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(OtpTemplate.Model)] = new OtpTemplateModel
                {
                    Token = token,
                    Link = link,
                    DisplayName = user.DisplayName!,
                },
                [nameof(HttpContext)] = HttpContext
            });

            var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var renderedComponent = await htmlRenderer.RenderComponentAsync<OtpTemplate>(parameters);

                return renderedComponent.ToHtmlString();
            });

            var emailResult = await fluentEmail.To(user.Email, user.DisplayName)
                                               .Subject(emailLocalizer[EmailStrings.OtpEmailSubject])
                                               .Body(body, isHtml: true)
                                               .SendAsync(cancellationToken);

            if (emailResult.Successful is false)
                throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => Localizer[err]).ToArray());
        }

        async Task SendSms()
        {
            if (await userManager.IsPhoneNumberConfirmedAsync(user) is false) return;

            await smsService.SendSms(Localizer[nameof(AppStrings.YourOtp), token], user.PhoneNumber!, cancellationToken);
        }

        await Task.WhenAll(SendEmail(), SendSms());
    }

    [HttpPost]
    public async Task ResetPassword(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        var user = await userManager.FindUser(resetPasswordRequest) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")]);

        bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, $"ResetPassword,Date:{user.ResetPasswordTokenRequestedOn}", resetPasswordRequest.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException();
        }

        var result = await userManager.ResetPasswordAsync(user!, await userManager.GeneratePasswordResetTokenAsync(user!), resetPasswordRequest.Password!);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendTwoFactorToken(IdentityRequestDto body, CancellationToken cancellationToken)
    {
        var user = await userManager.FindUser(body) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

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
                var templateParameters = new Dictionary<string, object?>()
                {
                    [nameof(TwoFactorTokenTemplate.Model)] = new TwoFactorTokenTemplateModel { DisplayName = user.DisplayName!, Token = token },
                    [nameof(HttpContext)] = HttpContext
                };

                var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                {
                    var renderedComponent = await htmlRenderer.RenderComponentAsync<TwoFactorTokenTemplate>(ParameterView.FromDictionary(templateParameters));

                    return renderedComponent.ToHtmlString();
                });

                var emailResult = await fluentEmail.To(user.Email, user.DisplayName)
                                                   .Subject(emailLocalizer[EmailStrings.TfaTokenEmailSubject])
                                                   .Body(body, isHtml: true)
                                                   .SendAsync(cancellationToken);

                if (emailResult.Successful is false)
                    throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => Localizer[err]).ToArray());
            }
        }

        async Task SendSms()
        {
            if (await userManager.IsPhoneNumberConfirmedAsync(user))
            {
                await smsService.SendSms(Localizer[nameof(AppStrings.YourTwoFactorToken), token], user.PhoneNumber!, cancellationToken);
            }
        }

        await Task.WhenAll(SendEmail(), SendSms());
    }



    private async Task SendConfirmEmailToken(SendEmailTokenRequestDto request, User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.EmailTokenRequestedOn) - AppSettings.IdentitySettings.EmailTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForEmailTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.EmailTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var email = request.Email!;
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyEmail:{user.Email},Date:{user.EmailTokenRequestedOn}");
        var link = new Uri(HttpContext.Request.GetBaseUrl(), $"confirm?email={Uri.EscapeDataString(email!)}&emailToken={Uri.EscapeDataString(token)}");
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>()
        {
            [nameof(EmailTokenTemplate.Model)] = new EmailTokenTemplateModel { Email = email, Token = token, Link = link },
            [nameof(HttpContext)] = HttpContext
        });

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<EmailTokenTemplate>(parameters);

            return renderedComponent.ToHtmlString();
        });

        var emailResult = await fluentEmail.To(user.Email, user.DisplayName)
                                           .Subject(emailLocalizer[nameof(EmailStrings.ConfirmationEmailSubject)])
                                           .Body(body, isHtml: true)
                                           .SendAsync(cancellationToken);

        if (emailResult.Successful is false)
            throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => Localizer[err]).ToArray());
    }

    private async Task SendConfirmPhoneToken(SendPhoneTokenRequestDto request, User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.PhoneNumberTokenRequestedOn) - AppSettings.IdentitySettings.PhoneNumberTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForPhoneNumberTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.PhoneNumberTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var phoneNumber = user.PhoneNumber;
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyPhoneNumber:{phoneNumber},Date:{user.PhoneNumberTokenRequestedOn}");
        var link = new Uri(HttpContext.Request.GetBaseUrl(), $"confirm?phoneNumber={Uri.EscapeDataString(phoneNumber!)}&phoneToken={Uri.EscapeDataString(token)}");

        await smsService.SendSms(Localizer[nameof(AppStrings.YourConfirmPhoneToken), token], user.PhoneNumber!, cancellationToken);
    }
}
