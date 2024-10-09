using System.Collections.Concurrent;
using Boilerplate.Shared.Dtos.PushNotification;
using WebPush;

namespace Boilerplate.Server.Api.Services;

public class DeviceInstallation : DeviceInstallationDto
{
    public Guid? UserId { get; set; }

    public string[] Tags { get; set; } = [];

    public DateTimeOffset? ExpirationTime { get; set; }
}

public class PushNotificationService(HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    AppSettings appSettings)
{
    private static ConcurrentDictionary<string, DeviceInstallation> deviceInstallations = new();

    public async Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        var userId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetUserId() : (Guid?)null;

        if (userId is not null)
        {
            tags.Add(userId.ToString()!);
        }

        deviceInstallations.Remove(deviceInstallation.InstallationId!, out _);

        deviceInstallations.TryAdd(deviceInstallation.InstallationId!, new()
        {
            Auth = deviceInstallation.Auth,
            Endpoint = deviceInstallation.Endpoint,
            InstallationId = deviceInstallation.InstallationId,
            P256dh = deviceInstallation.P256dh,
            Platform = deviceInstallation.Platform,
            PushChannel = deviceInstallation.PushChannel,
            UserId = userId,
            Tags = [.. tags],
            ExpirationTime = DateTimeOffset.UtcNow.AddMonths(1).DateTime
        });
    }

    public async Task RequestPush(string? title = null, string? message = null, string? action = null, string[]? tags = null, CancellationToken cancellationToken = default)
    {
        tags ??= [];

        foreach (var deviceInstallation in deviceInstallations.Values)
        {
            if (tags.Any() && deviceInstallation.Tags.Any(dt => tags.Contains(dt)) is false)
                continue;

            if (deviceInstallation.Platform is not "browser")
                continue;

            var subscription = new PushSubscription(deviceInstallation.Endpoint, deviceInstallation.P256dh, deviceInstallation.Auth);
            var vapidDetails = new VapidDetails(appSettings.PushNotification.WebPushSubject, appSettings.PushNotification.WebPushVapidPublicKey, appSettings.PushNotification.WebPushVapidPrivateKey);

            using var webPushClient = new WebPushClient();
            await webPushClient.SendNotificationAsync(subscription, "{ \"title\": \"title2\", \"body\": \"body2\", \"action\": \"action\" }", vapidDetails, cancellationToken);
        }
    }
}
