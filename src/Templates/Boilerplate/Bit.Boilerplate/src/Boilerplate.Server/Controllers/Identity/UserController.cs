using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using QRCoder;
using FluentEmail.Core;
using Boilerplate.Shared;
using Boilerplate.Server.Resources;
using Boilerplate.Server.Components;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Models.Emailing;
using Boilerplate.Server.Models.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Server.Controllers.Identity;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController]
public partial class UserController : AppControllerBase, IUserController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private IUserStore<User> userStore = default!;

    [AutoInject] private UrlEncoder urlEncoder = default!;

    [AutoInject] private HtmlRenderer htmlRenderer = default!;

    [AutoInject] private IFluentEmail fluentEmail = default!;

    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    [HttpGet]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        return user.Map();
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
    public async Task ChangePassword(ChangePasswordRequestDto body, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        if (await userManager.IsLockedOutAsync(user!))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.ToString("mm\\:ss")]);

        var result = await userManager.ChangePasswordAsync(user!, body.OldPassword!, body.NewPassword!);

        if (result.Succeeded is false)
        {
            await userManager.AccessFailedAsync(user!);

            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
        }
    }

    [HttpPost]
    public async Task ChangeUserName(ChangeUserNameRequestDto body, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
        var result = await userManager.SetUserNameAsync(user!, body.UserName);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendChangeEmailToken(SendEmailTokenRequestDto sendEmailTokenRequest, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var resendDelay = (DateTimeOffset.Now - user!.EmailTokenRequestedOn) - AppSettings.IdentitySettings.EmailTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForEmailTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.EmailTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, $"ChangeEmail:{sendEmailTokenRequest.Email}");

        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<EmailTokenTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
            {
                {   nameof(EmailTokenTemplate.Model),
                    new EmailTokenTemplateModel
                    {
                        Token = token,
                        Email = sendEmailTokenRequest.Email
                    }
                },
                { nameof(HttpContext), HttpContext }
            }));

            return renderedComponent.ToHtmlString();
        });

        var emailResult = await fluentEmail.To(user.Email, user.DisplayName)
                                           .Subject(emailLocalizer[EmailStrings.ConfirmationEmailSubject])
                                           .Body(body, isHtml: true)
                                           .SendAsync(cancellationToken);

        if (emailResult.Successful is false)
            throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => Localizer[err]).ToArray());
    }

    [HttpPost]
    public async Task ChangeEmail(ChangeEmailRequestDto body, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var tokenIsVerified = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, $"ChangeEmail:{body.Email}", body.Token!);

        if (tokenIsVerified)
            throw new BadRequestException();

        await ((IUserEmailStore<User>)userStore).SetEmailAsync(user!, body.Email, cancellationToken);
        var result = await userManager.UpdateAsync(user!);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendChangePhoneNumberToken(SendPhoneNumberTokenRequestDto body, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var resendDelay = (DateTimeOffset.Now - user!.PhoneNumberTokenRequestedOn) - AppSettings.IdentitySettings.PhoneNumberTokenRequestResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForPhoneNumberTokenRequestResendDelay), resendDelay.Value.ToString("mm\\:ss")]);

        user.PhoneNumberTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user!, body.PhoneNumber!);

        // TODO: Send token through SMS
    }

    [HttpPost]
    public async Task ChangePhoneNumber(ChangePhoneNumberRequestDto body, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var result = await userManager.ChangePhoneNumberAsync(user!, body.PhoneNumber!, body.Token!);

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

    [HttpPost, Microsoft.AspNetCore.Mvc.Route("~/api/[controller]/2fa")]
    public async Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto tfaRequest, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ResourceNotFoundException();

        if (tfaRequest.Enable is true)
        {
            if (tfaRequest.ResetSharedKey)
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaResetSharedKeyError)]);
            else if (string.IsNullOrEmpty(tfaRequest.TwoFactorCode))
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaEmptyCodeError)]);
            else if (await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, tfaRequest.TwoFactorCode) is false)
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
