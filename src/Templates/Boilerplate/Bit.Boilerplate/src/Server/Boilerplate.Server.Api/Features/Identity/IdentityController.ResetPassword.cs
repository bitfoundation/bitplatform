//+:cnd:noEmit
using Humanizer;
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Server.Api.Features.Identity.Models;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
//#endif

namespace Boilerplate.Server.Api.Features.Identity;

public partial class IdentityController
{
    [HttpPost]
    public async Task SendResetPasswordToken(SendResetPasswordTokenRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindUser(request)
                    ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]).WithData("Identifier", request);

        if (await userConfirmation.IsConfirmedAsync(userManager, user) is false)
        {
            await SendConfirmationToken(user, request.ReturnUrl, cancellationToken);

            throw new BadRequestException(Localizer[nameof(AppStrings.UserIsNotConfirmed)]).WithData("UserId", user.Id);
        }

        var resendDelay = (DateTimeOffset.Now - user.ResetPasswordTokenRequestedOn) - AppSettings.Identity.ResetPasswordTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsException(Localizer[nameof(AppStrings.WaitForResetPasswordTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", resendDelay);

        user.ResetPasswordTokenRequestedOn = DateTimeOffset.Now;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"ResetPassword,{user.ResetPasswordTokenRequestedOn?.ToUniversalTime()}"));
        var isEmail = string.IsNullOrEmpty(request.Email) is false;
        var qs = $"{(isEmail ? "email" : "phoneNumber")}={Uri.EscapeDataString(isEmail ? request.Email! : request.PhoneNumber!)}";
        var url = $"{PageUrls.ResetPassword}?token={Uri.EscapeDataString(token)}&{qs}&culture={CultureInfo.CurrentUICulture.Name}&return-url={Uri.EscapeDataString(request.ReturnUrl ?? PageUrls.Home)}";
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
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!));
        }

        //#if (signalR == true)
        var userConnectionIds = await DbContext.UserSessions
            .Where(us => us.NotificationStatus == UserSessionNotificationStatus.Allowed && us.UserId == user.Id)
            .Select(us => us.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
        sendMessagesTasks.Add(appHubContext.Clients.Clients(userConnectionIds).SendAsync(SharedAppMessages.SHOW_MESSAGE, message, null, cancellationToken));
        //#endif

        //#if (notification == true)
        sendMessagesTasks.Add(pushNotificationService.RequestPush(new()
        {
            Message = message,
            UserRelatedPush = true
        }, customSubscriptionFilter: s => s.UserSession!.UserId == user.Id, cancellationToken: cancellationToken));
        //#endif

        await Task.WhenAll(sendMessagesTasks);
    }

    [HttpPost]
    public async Task ResetPassword(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindUser(request) ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserNotFound)]).WithData("Identifier", request);

        var expired = (DateTimeOffset.Now - user.ResetPasswordTokenRequestedOn) > AppSettings.Identity.ResetPasswordTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken)).WithData("UserId", user.Id);

        if (await userManager.IsLockedOutAsync(user))
        {
            var tryAgainIn = (user.LockoutEnd! - DateTimeOffset.UtcNow).Value;
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), tryAgainIn.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", tryAgainIn);
        }

        bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"ResetPassword,{user.ResetPasswordTokenRequestedOn?.ToUniversalTime()}"), request.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException(nameof(AppStrings.InvalidToken)).WithData("UserId", user.Id);
        }

        var result = await userManager.ResetPasswordAsync(user!, await userManager.GeneratePasswordResetTokenAsync(user!), request.Password!);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.ResetPasswordTokenRequestedOn = null; // invalidates reset password token
        var updateResult = await userManager.UpdateAsync(user);
        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);
    }
}
