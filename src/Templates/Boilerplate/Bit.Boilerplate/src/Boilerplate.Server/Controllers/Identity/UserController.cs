using Boilerplate.Shared;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Models.Identity;
using Boilerplate.Client.Core.Controllers.Identity;
using QRCoder;
using System.Text.Encodings.Web;
using System.Text;

namespace Boilerplate.Server.Controllers.Identity;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class UserController : AppControllerBase, IUserController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private UrlEncoder urlEncoder = default!;

    [HttpGet]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ResourceNotFoundException();

        return user.Map();
    }

    [HttpPut]
    public async Task<UserDto> Update(EditUserDto userDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ResourceNotFoundException();

        userDto.Patch(user);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        return await GetCurrentUser(cancellationToken);
    }

    [HttpPost]
    public async Task ChangePassword(ChangePasswordRequestDto body, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
        string token = await userManager.GeneratePasswordResetTokenAsync(user!);
        var result = await userManager.ResetPasswordAsync(user!, token, body.Password!);
        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPost]
    public async Task ChangeUserName(ChangeUserNameRequestDto body, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
        var result = await userManager.SetUserNameAsync(user!, body.UserName);
        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpDelete]
    public async Task Delete(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPost, Route("~/api/[controller]/manage/2fa")]
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
