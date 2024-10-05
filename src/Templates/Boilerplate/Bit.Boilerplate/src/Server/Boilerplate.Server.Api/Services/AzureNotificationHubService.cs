using Microsoft.Azure.NotificationHubs;
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Server.Api.Models.PushNotification;

namespace Boilerplate.Server.Api.Services;

public class AzureNotificationHubService(IHttpContextAccessor httpContextAccessor,
    AppSettings appSettings,
    NotificationHubClient? hub = null)
{
    private static readonly Dictionary<string, NotificationPlatform> installationPlatform = new()
    {
        { nameof(NotificationPlatform.Apns).ToLower(), NotificationPlatform.Apns},
        { nameof(NotificationPlatform.FcmV1).ToLower(), NotificationPlatform.FcmV1 }
    };

    public async Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        if (httpContextAccessor.HttpContext!.User.IsAuthenticated())
        {
            tags.Add(httpContextAccessor.HttpContext.User.GetUserId().ToString()); // To send push notification to a specific user
        }

        var installation = new Installation()
        {
            InstallationId = deviceInstallation.InstallationId,
            PushChannel = deviceInstallation.PushChannel,
            Tags = tags
        };

        if (installationPlatform.TryGetValue(deviceInstallation.Platform!, out var platform))
        {
            installation.Platform = platform;
        }

        if (appSettings.NotificationHub.Configured is false)
            return;

        await hub!.CreateOrUpdateInstallationAsync(installation, cancellationToken);
    }

    public async Task DeleteInstallation([Required] string installationId, CancellationToken cancellationToken)
    {
        if (appSettings.NotificationHub.Configured is false)
            return;

        await hub!.DeleteInstallationAsync(installationId, cancellationToken);
    }

    public async Task RequestPush(string? text = null, string? action = null, string[]? tags = null, bool silent = false, CancellationToken cancellationToken = default)
    {
        if (appSettings.NotificationHub.Configured is false)
            return;

        tags ??= [];

        if ((silent && string.IsNullOrWhiteSpace(action)) ||
            (!silent && string.IsNullOrWhiteSpace(text)))
            throw new BadRequestException();

        var androidPushTemplate = silent ?
            PushTemplates.Silent.Android :
            PushTemplates.Generic.Android;

        var iOSPushTemplate = silent ?
            PushTemplates.Silent.iOS :
            PushTemplates.Generic.iOS;

        var androidPayload = PrepareNotificationPayload(
            androidPushTemplate,
            text!,
            action!);

        var iOSPayload = PrepareNotificationPayload(
            iOSPushTemplate,
            text!,
            action!);

        if (tags.Any() is false)
        {
            // This will broadcast to all users registered in the notification hub
            await SendPlatformNotificationsAsync(androidPayload, iOSPayload, cancellationToken);
        }
        else if (tags.Length <= 20)
        {
            await SendPlatformNotificationsAsync(androidPayload, iOSPayload, tags, cancellationToken);
        }
        else
        {
            var notificationTasks = tags
                .Select((value, index) => (value, index))
            .GroupBy(g => g.index / 20, i => i.value)
                .Select(tags => SendPlatformNotificationsAsync(androidPayload, iOSPayload, tags, cancellationToken));

            await Task.WhenAll(notificationTasks);
        }
    }

    private string PrepareNotificationPayload(string template, string text, string action) => template
        .Replace("$(alertMessage)", text, StringComparison.InvariantCulture)
        .Replace("$(alertAction)", action, StringComparison.InvariantCulture);

    private async Task SendPlatformNotificationsAsync(string androidPayload, string iOSPayload, CancellationToken cancellationToken)
    {
        await (hub!.SendFcmV1NativeNotificationAsync(androidPayload, cancellationToken),
            hub.SendAppleNativeNotificationAsync(iOSPayload, cancellationToken));
    }

    private async Task SendPlatformNotificationsAsync(string androidPayload, string iOSPayload, IEnumerable<string> tags, CancellationToken cancellationToken)
    {
        await (hub!.SendFcmV1NativeNotificationAsync(androidPayload, tags, cancellationToken),
            hub.SendAppleNativeNotificationAsync(iOSPayload, tags, cancellationToken));
    }
}
