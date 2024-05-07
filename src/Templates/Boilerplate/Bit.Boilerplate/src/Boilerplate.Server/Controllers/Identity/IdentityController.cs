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

    [HttpPost]
    public async Task<ActionResult<SignInResponseDto>> SignIn(SignInRequestDto signInRequest, CancellationToken cancellationToken)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(signInRequest.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var user = await userManager.FindByNameAsync(signInRequest.UserName!);
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.ToString("mm\\:ss")));
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
                //var isValid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, token);
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
                    ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), sendResetPasswordEmailRequest.Email!));

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
        var user = await userManager.FindByEmailAsync(resetPasswordRequest.Email!)
                    ?? throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), resetPasswordRequest.Email!));

        var result = await userManager.ResetPasswordAsync(user, resetPasswordRequest.Token!, resetPasswordRequest.Password!);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendTwoFactorTokenEmail(SignInRequestDto signInRequest, CancellationToken cancellationToken)
    {
        var signInResult = await signInManager.PasswordSignInAsync(signInRequest.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);
        var user = await userManager.FindByNameAsync(signInRequest.UserName!) ?? throw new ResourceNotFoundException();

        if (signInResult.IsLockedOut)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.ToString("mm\\:ss")));

        if (signInResult.RequiresTwoFactor is false)
        {
            throw new BadRequestException();
        }

        var resendDelay = (DateTimeOffset.Now - user.TwoFactorTokenEmailRequestedOn) - AppSettings.IdentitySettings.TwoFactorTokenEmailResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForTfaTokenEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

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

        user.TwoFactorTokenEmailRequestedOn = DateTimeOffset.Now;

        await userManager.UpdateAsync(user);

        if (emailResult.Successful is false)
            throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => Localizer[err]).ToArray());
    }

    [Authorize, HttpPost]
    [Microsoft.AspNetCore.Mvc.Route("~/api/[controller]/2fa")]
    public async Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto tfaRequest, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ResourceNotFoundException();

        if (tfaRequest.Enable is true)
        {
            if (tfaRequest.ResetSharedKey)
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaResetSharedKeyError)]);
            else if (string.IsNullOrEmpty(tfaRequest.TwoFactorCode))
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaEmptyCodeError)]);
            else if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, tfaRequest.TwoFactorCode))
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaInvalidCodeError)]);

            await userManager.SetTwoFactorEnabledAsync(user, true);
        }
        else if (tfaRequest.Enable is false || tfaRequest.ResetSharedKey)
        {
            await userManager.SetTwoFactorEnabledAsync(user, false);
        }

        if (tfaRequest.ResetSharedKey)
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
        }

        string[]? recoveryCodes = null;
        if (tfaRequest.ResetRecoveryCodes || (tfaRequest.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0))
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
        var authenticatorUri = GenerateQrCodeUri(user.Email!, unformattedKey);

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
    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(CultureInfo.InvariantCulture,
                             AUTHENTICATOR_URI_FORMAT,
                             urlEncoder.Encode("bit platform Boilerplate"),
                             urlEncoder.Encode(email),
                             unformattedKey);
    }
}
