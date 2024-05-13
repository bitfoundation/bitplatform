//+:cnd:noEmit
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Authentication.BearerToken;
using QRCoder;
using FluentEmail.Core;
using Boilerplate.Server.Services;
using Boilerplate.Server.Resources;
using Boilerplate.Server.Components;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Models.Emailing;
using Boilerplate.Server.Models.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Server.Controllers.Identity;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private SignInManager<User> signInManager = default!;

    [AutoInject] private IUserStore<User> userStore = default!;

    [AutoInject] private IFluentEmail fluentEmail = default!;

    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    [AutoInject] private HtmlRenderer htmlRenderer = default!;

    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    [AutoInject] private UrlEncoder urlEncoder = default!;


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

        var existingUser = await FindUser(signUpRequest.UserName, signUpRequest.Email, signUpRequest.PhoneNumber, cancellationToken, throwErrorOnNotFound: false);

        var userToAdd = new User { };

        if (existingUser is not null)
        {
            if (await userManager.IsEmailConfirmedAsync(existingUser) || await userManager.IsPhoneNumberConfirmedAsync(existingUser))
            {
                throw new BadRequestException(Localizer.GetString(nameof(AppStrings.DuplicateEmailOrPhoneNumber)));
            }
            else
            {
                var deleteResult = await userManager.DeleteAsync(existingUser);
                if (!deleteResult.Succeeded)
                    throw new ResourceValidationException(deleteResult.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
                if (string.IsNullOrEmpty(signUpRequest.Email) is false)
                {
                    userToAdd.ConfirmationEmailRequestedOn = existingUser.ConfirmationEmailRequestedOn;
                }
                if (string.IsNullOrEmpty(signUpRequest.PhoneNumber) is false)
                {
                    userToAdd.ConfirmationSmsRequestedOn = existingUser.ConfirmationSmsRequestedOn;
                }
            }
        }

        userToAdd.LockoutEnabled = true;

        await userStore.SetUserNameAsync(userToAdd, signUpRequest.UserName!, cancellationToken);
        await ((IUserEmailStore<User>)userStore).SetEmailAsync(userToAdd, signUpRequest.Email!, cancellationToken);
        await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberAsync(userToAdd, signUpRequest.PhoneNumber!, cancellationToken);

        var result = await userManager.CreateAsync(userToAdd, signUpRequest.Password!);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        if (string.IsNullOrEmpty(userToAdd.Email) is false)
        {
            await SendConfirmationEmail(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
        }

        if (string.IsNullOrEmpty(userToAdd.PhoneNumber) is false)
        {
            await SendConfirmationSms(new() { PhoneNumber = userToAdd.PhoneNumber }, userToAdd, cancellationToken);
        }
    }

    [HttpPost]
    public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(sendConfirmationEmailRequest.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound), sendConfirmationEmailRequest.Email!]);

        if (await userManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)]);

        await SendConfirmationEmail(sendConfirmationEmailRequest, user, cancellationToken);
    }

    private async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.ConfirmationEmailRequestedOn) - AppSettings.IdentitySettings.ConfirmationEmailResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForConfirmationEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyEmail:{user.Email}");

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<EmailConfirmationTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
            {
                {   nameof(EmailConfirmationTemplate.Model),
                    new EmailConfirmationModel
                    {
                        Token = token
                    }
                },
                { nameof(HttpContext), HttpContext }
            }));

            return renderedComponent.ToHtmlString();
        });

        var result = await fluentEmail
                           .To(user.Email, user.DisplayName)
                           .Subject(emailLocalizer[EmailStrings.ConfirmationEmailSubject])
                           .Body(body, isHtml: true)
                           .SendAsync(cancellationToken);

        user.ConfirmationEmailRequestedOn = DateTimeOffset.Now;

        await userManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.Select(err => Localizer[err]).ToArray());
    }

    [HttpPost]
    public async Task ConfirmEmail(ConfirmEmailRequestDto body, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(body.Email!)
            ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNotFound), body.Email!));

        if (user.EmailConfirmed is false)
        {
            var result = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyEmail:{body.Email}", body.Token!);

            if (result)
            {
                var userEmailStore = (IUserEmailStore<User>)userStore;
                await userEmailStore.SetEmailConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }
            else
            {
                throw new BadRequestException();
            }
        }
    }

    [HttpPost]
    public async Task SendConfirmationSms(SendConfirmationSmsRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber!, cancellationToken)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound), request.PhoneNumber!]);

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.PhoneNumberAlreadyConfirmed)]);

        await SendConfirmationSms(request, user, cancellationToken);
    }

    private async Task SendConfirmationSms(SendConfirmationSmsRequestDto request, User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.ConfirmationSmsRequestedOn) - AppSettings.IdentitySettings.ConfirmationSmsResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForConfirmationSmsResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyPhoneNumber:{user.PhoneNumber}");

        // TODO: Send token through SMS

        user.ConfirmationSmsRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task ConfirmPhoneNumber(ConfirmPhoneNumberRequestDto body, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == body.PhoneNumber!, cancellationToken)
            ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNotFound), body.PhoneNumber!));

        if (user.PhoneNumberConfirmed is false)
        {
            var result = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, $"VerifyPhoneNumber:{body.PhoneNumber}", body.Token!);

            if (result)
            {
                await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }
            else
            {
                throw new BadRequestException();
            }
        }
    }

    [HttpPost]
    public async Task<ActionResult<SignInResponseDto>> SignIn(SignInRequestDto signInRequest, CancellationToken cancellationToken)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var user = await FindUser(signInRequest.UserName, signInRequest.Email, signInRequest.PhoneNumber, cancellationToken, throwErrorOnNotFound: true);

        var result = await signInManager.PasswordSignInAsync(user!.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")));
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
            throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUsernameOrPassword)]);

        return Empty;
    }

    private async Task<User?> FindUser(string? userName, string? email, string? phoneNumber, CancellationToken cancellationToken, bool throwErrorOnNotFound)
    {
        User? user = default;

        if (userName is null && email is null && phoneNumber is null)
            throw new InvalidOperationException();

        if (string.IsNullOrEmpty(userName) is false)
        {
            user = await userManager.FindByNameAsync(userName!);
        }
        else if (string.IsNullOrEmpty(email) is false)
        {
            user = await userManager.FindByEmailAsync(email!);
        }
        else if (string.IsNullOrEmpty(phoneNumber) is false)
        {
            user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
        }

        if (user is null && throwErrorOnNotFound)
        {
            throw new ResourceNotFoundException(Localizer.GetString(nameof(AppStrings.UserNotFound), email ?? phoneNumber ?? userName!));
        }

        return user;
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
    public async Task SendResetPasswordEmail(SendResetPasswordEmailRequestDto sendResetPasswordEmailRequest, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(sendResetPasswordEmailRequest.Email!)
                    ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNotFound), sendResetPasswordEmailRequest.Email!));

        var resendDelay = (DateTimeOffset.Now - user.ResetPasswordEmailRequestedOn) - AppSettings.IdentitySettings.ResetPasswordEmailResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForResetPasswordEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var resetPasswordLink = new Uri(HttpContext.Request.GetBaseUrl(), $"reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}");

        var templateParameters = new Dictionary<string, object?>()
        {
            [nameof(ResetPasswordTemplate.Model)] = new ResetPasswordModel { DisplayName = user.DisplayName, ResetPasswordLink = resetPasswordLink },
            [nameof(HttpContext)] = HttpContext
        };

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<ResetPasswordTemplate>(ParameterView.FromDictionary(templateParameters));

            return renderedComponent.ToHtmlString();
        });

        var result = await fluentEmail.To(user.Email, user.DisplayName)
                                      .Subject(emailLocalizer[EmailStrings.ResetPasswordEmailSubject])
                                      .Body(body, isHtml: true)
                                      .SendAsync(cancellationToken);

        user.ResetPasswordEmailRequestedOn = DateTimeOffset.Now;

        await userManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.Select(err => Localizer[err]).ToArray());
    }

    [HttpPost]
    public async Task ResetPassword(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    {
        var user = await FindUser(resetPasswordRequest.UserName, resetPasswordRequest.Email, resetPasswordRequest.PhoneNumber, cancellationToken, throwErrorOnNotFound: true);

        // TODO: TEST
        var result = await userManager.ResetPasswordAsync(user, resetPasswordRequest.Token!, resetPasswordRequest.Password!);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendTwoFactorToken(SignInRequestDto signInRequest, CancellationToken cancellationToken)
    {
        var user = await FindUser(signInRequest.UserName, signInRequest.Email, signInRequest.PhoneNumber, cancellationToken, throwErrorOnNotFound: true);

        var signInResult = await signInManager.PasswordSignInAsync(user.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.ToString("mm\\:ss")));

        if (signInResult.RequiresTwoFactor is false)
        {
            throw new BadRequestException();
        }

        var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            var resendDelay = (DateTimeOffset.Now - user.TwoFactorTokenRequestedOn) - AppSettings.IdentitySettings.TwoFactorTokenEmailResendDelay;

            if (resendDelay < TimeSpan.Zero)
                throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForTfaTokenEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

            var templateParameters = new Dictionary<string, object?>()
            {
                [nameof(TwoFactorTokenTemplate.Model)] = new TwoFactorTokenModel { DisplayName = user.DisplayName ?? "User", Token = token },
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

            user.TwoFactorTokenRequestedOn = DateTimeOffset.Now;

            await userManager.UpdateAsync(user);

            if (emailResult.Successful is false)
                throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => Localizer[err]).ToArray());
        }

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            // TODO: Send token through sms
        }
    }
}
