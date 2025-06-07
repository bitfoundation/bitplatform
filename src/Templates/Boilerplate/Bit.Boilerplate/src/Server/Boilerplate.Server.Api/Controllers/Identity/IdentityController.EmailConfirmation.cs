//+:cnd:noEmit
using Humanizer;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Server.Api.Controllers.Identity;

public partial class IdentityController
{
    [AutoInject] private EmailService emailService = default!;

    [HttpPost]
    public async Task SendConfirmEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]).WithData("Email", request.Email);

        if (await userManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)]).WithData("UserId", user.Id);

        await SendConfirmEmailToken(user, request.ReturnUrl, cancellationToken);
    }

    [HttpPost, Produces<TokenResponseDto>()]
    public async Task ConfirmEmail(ConfirmEmailRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]).WithData("Email", request.Email);

        var expired = (DateTimeOffset.Now - user.EmailTokenRequestedOn) > AppSettings.Identity.EmailTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken)).WithData("UserId", user.Id);

        if (await userManager.IsLockedOutAsync(user))
        {
            var tryAgainIn = (user.LockoutEnd! - DateTimeOffset.UtcNow).Value;
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", tryAgainIn);
        }

        var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"VerifyEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"), request.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException(nameof(AppStrings.InvalidToken)).WithData("UserId", user.Id);
        }

        await userEmailStore.SetEmailConfirmedAsync(user, true, cancellationToken);
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.EmailTokenRequestedOn = null; // invalidates email token
        var updateResult = await userManager.UpdateAsync(user);
        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_Email,{user.OtpRequestedOn?.ToUniversalTime()}"));

        await SignIn(new() { Email = request.Email, Otp = token }, cancellationToken);
    }


    private async Task SendConfirmEmailToken(User user, string? returnUrl, CancellationToken cancellationToken)
    {
        returnUrl ??= Urls.HomePage;

        var resendDelay = (DateTimeOffset.Now - user.EmailTokenRequestedOn) - AppSettings.Identity.EmailTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForEmailTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", resendDelay);

        user.EmailTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        var email = user.Email!;
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"VerifyEmail:{email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"));
        var link = new Uri(HttpContext.Request.GetWebAppUrl(), $"{Urls.ConfirmPage}?email={Uri.EscapeDataString(email)}&emailToken={Uri.EscapeDataString(token)}&culture={CultureInfo.CurrentUICulture.Name}&return-url={Uri.EscapeDataString(returnUrl)}");

        await emailService.SendEmailToken(user, email, token, link, cancellationToken);
    }
}
