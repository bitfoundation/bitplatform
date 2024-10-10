using AdsPush;
using AdsPush.Vapid;
using AdsPush.Abstraction;
using System.Collections.Concurrent;
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Server.Api.Models.PushNotification;

namespace Boilerplate.Server.Api.Services;

public partial class PushNotificationService
{
    [AutoInject] private IAdsPushSenderFactory adsPushSenderFactory = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

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

        var sender = adsPushSenderFactory.GetSender("Primary");

        foreach (var deviceInstallation in deviceInstallations.Values)
        {
            if (tags.Any() && deviceInstallation.Tags.Any(dt => tags.Contains(dt)) is false)
                continue;

            if (deviceInstallation.Platform is not "browser")
                continue;

            var subscription = VapidSubscription.FromParameters(deviceInstallation.Endpoint, deviceInstallation.P256dh, deviceInstallation.Auth);

            await sender.BasicSendAsync(AdsPushTarget.BrowserAndPwa, subscription.ToAdsPushToken(), new()
            {
                Title = AdsPushText.CreateUsingString(title),
                Detail = AdsPushText.CreateUsingString(message),
                Parameters = new Dictionary<string, object>()
                {
                    {
                        "action", action ?? string.Empty
                    }
                }
            }, cancellationToken);
        }
    }
}
