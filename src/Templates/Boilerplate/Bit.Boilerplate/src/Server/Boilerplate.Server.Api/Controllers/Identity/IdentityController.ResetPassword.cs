//+:cnd:noEmit
using Humanizer;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Api.Models.Identity;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
//#endif

namespace Boilerplate.Server.Api.Controllers.Identity;

public partial class IdentityController
{
    [HttpPost]
    public async Task SendResetPasswordToken(SendResetPasswordTokenRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindUserAsync(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        if (await userConfirmation.IsConfirmedAsync(userManager, user) is false)
            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]);

        var resendDelay = (DateTimeOffset.Now - user.ResetPasswordTokenRequestedOn) - AppSettings.Identity.ResetPasswordTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForResetPasswordTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        user.ResetPasswordTokenRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"ResetPassword,{user.ResetPasswordTokenRequestedOn?.ToUniversalTime()}"));
        var isEmail = string.IsNullOrEmpty(request.Email) is false;
        var qs = $"{(isEmail ? "email" : "phoneNumber")}={Uri.EscapeDataString(isEmail ? request.Email! : request.PhoneNumber!)}";
        var url = $"{Urls.ResetPasswordPage}?token={Uri.EscapeDataString(token)}&{qs}&culture={CultureInfo.CurrentUICulture.Name}";
        var link = new Uri(HttpContext.Request.GetWebAppUrl(), url);

        List<Task> sendMessagesTasks = [];

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendResetPasswordToken(user, token, link, cancellationToken));
        }

        var message = Localizer[nameof(AppStrings.ResetPasswordTokenShortText), token].ToString();

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!, cancellationToken));
        }

        //#if (signalR == true)
        sendMessagesTasks.Add(appHubContext.Clients.User(user.Id.ToString()).SendAsync(SignalREvents.SHOW_MESSAGE, message, cancellationToken));
        //#endif

        //#if (notification == true)
        sendMessagesTasks.Add(pushNotificationService.RequestPush(message: message, userRelatedPush: true, customSubscriptionFilter: s => s.UserSession!.UserId == user.Id, cancellationToken: cancellationToken));
        //#endif

        await Task.WhenAll(sendMessagesTasks);
    }

    [HttpPost]
    public async Task ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindUserAsync(request) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]);

        var expired = (DateTimeOffset.Now - user.ResetPasswordTokenRequestedOn) > AppSettings.Identity.ResetPasswordTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken));

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.Humanize(culture: CultureInfo.CurrentUICulture)]);

        bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"ResetPassword,{user.ResetPasswordTokenRequestedOn?.ToUniversalTime()}"), request.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException(nameof(AppStrings.InvalidToken));
        }

        var result = await userManager.ResetPasswordAsync(user!, await userManager.GeneratePasswordResetTokenAsync(user!), request.Password!);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.ResetPasswordTokenRequestedOn = null; // invalidates reset password token
        var updateResult = await userManager.UpdateAsync(user);
        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }
}
