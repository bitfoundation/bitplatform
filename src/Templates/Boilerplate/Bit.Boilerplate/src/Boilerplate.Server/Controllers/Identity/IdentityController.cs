//-:cnd:noEmit
using System.Text;
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

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private SignInManager<User> signInManager = default!;

    [AutoInject] private IFluentEmail fluentEmail = default!;

    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    [AutoInject] private HtmlRenderer htmlRenderer = default!;

    [AutoInject] private IStringLocalizer<IdentityStrings> identityLocalizer = default!;

    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    [AutoInject] private GoogleRecaptchaHttpClient googleRecaptchaHttpClient = default!;

    /// <summary>
    /// By leveraging summary tags in your controller's actions and DTO properties you can make your codes much easier to maintain.
    /// These comments will also be used in swagger docs and ui.
    /// </summary>
    [HttpPost]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        if (await googleRecaptchaHttpClient.Verify(signUpRequest.GoogleRecaptchaResponse) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.InvalidGoogleRecaptchaResponse)]);

        var existingUser = await userManager.FindByNameAsync(signUpRequest.Email!);

        var userToAdd = signUpRequest.Map();

        if (existingUser is not null)
        {
            if (await userManager.IsEmailConfirmedAsync(existingUser))
            {
                throw new BadRequestException(Localizer.GetString(nameof(AppStrings.DuplicateEmail), existingUser.Email!));
            }
            else
            {
                var deleteResult = await userManager.DeleteAsync(existingUser);
                if (!deleteResult.Succeeded)
                    throw new ResourceValidationException(deleteResult.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
                userToAdd.ConfirmationEmailRequestedOn = existingUser.ConfirmationEmailRequestedOn;
            }
        }

        userToAdd.LockoutEnabled = true;

        var result = await userManager.CreateAsync(userToAdd, signUpRequest.Password!);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        await SendConfirmationEmail(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
    }

    [HttpPost]
    public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(sendConfirmationEmailRequest.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNameNotFound), sendConfirmationEmailRequest.Email!]);
        if (await userManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)]);

        await SendConfirmationEmail(sendConfirmationEmailRequest, user, cancellationToken);
    }

    private async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.ConfirmationEmailRequestedOn) - AppSettings.IdentitySettings.ConfirmationEmailResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForConfirmationEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var controller = RouteData.Values["controller"]!.ToString();

        var confirmationLink = new Uri(HttpContext.Request.GetBaseUrl(), $"email-confirmation?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}");

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<EmailConfirmationTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
            {
                {   nameof(EmailConfirmationTemplate.Model),
                    new EmailConfirmationModel
                    {
                        ConfirmationLink = confirmationLink
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
    public async Task ConfirmEmail(ConfirmEmailRequestDto body)
    {
        var user = await userManager.FindByEmailAsync(body.Email!)
            ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), body.Email!));
        var emailConfirmed = user.EmailConfirmed;

        if (emailConfirmed is false)
        {
            var result = await userManager.ConfirmEmailAsync(user, body.Token!);
            if (!result.Succeeded)
                throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }
    }

    [HttpPost, ProducesResponseType<TokenResponseDto>(statusCode: 200)]
    public async Task SignIn(SignInRequestDto signInRequest)
    {
        if (await googleRecaptchaHttpClient.Verify(signInRequest.GoogleRecaptchaResponse) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.InvalidGoogleRecaptchaResponse)]);

        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(signInRequest.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var user = await userManager.FindByNameAsync(signInRequest.UserName!);
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.ToString("mm\\:ss")));
        }

        /* if (result.RequiresTwoFactor)
        {
            if (!string.IsNullOrEmpty(signInRequest.TwoFactorCode))
            {
                result = await signInManager.TwoFactorAuthenticatorSignInAsync(signInRequest.TwoFactorCode, rememberClient: true);
            }
            else if (!string.IsNullOrEmpty(signInRequest.TwoFactorRecoveryCode))
            {
                result = await signInManager.TwoFactorRecoveryCodeSignInAsync(signInRequest.TwoFactorRecoveryCode);
            }
        } */

        if (result.Succeeded is false)
            throw new UnauthorizedException(Localizer[nameof(AppStrings.InvalidUsernameOrPassword)]);
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
        if (await googleRecaptchaHttpClient.Verify(sendResetPasswordEmailRequest.GoogleRecaptchaResponse) is false)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.InvalidGoogleRecaptchaResponse)));

        var user = await userManager.FindByEmailAsync(sendResetPasswordEmailRequest.Email!)
                    ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), sendResetPasswordEmailRequest.Email!));

        var resendDelay = (DateTimeOffset.Now - user.ResetPasswordEmailRequestedOn) - AppSettings.IdentitySettings.ResetPasswordEmailResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForResetPasswordEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var resetPasswordLink = new Uri(HttpContext.Request.GetBaseUrl(), $"reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}");

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<ResetPasswordTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
            {
                { nameof(ResetPasswordTemplate.Model),
                    new ResetPasswordModel
                    {
                        DisplayName = user.DisplayName,
                        ResetPasswordLink = resetPasswordLink
                    }
                },
                { nameof(HttpContext) , HttpContext }
            }));

            return renderedComponent.ToHtmlString();
        });

        var result = await fluentEmail
                           .To(user.Email, user.DisplayName)
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
        var user = await userManager.FindByEmailAsync(resetPasswordRequest.Email!)
                    ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), resetPasswordRequest.Email!));

        var result = await userManager.ResetPasswordAsync(user, resetPasswordRequest.Token!, resetPasswordRequest.Password!);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }
}
