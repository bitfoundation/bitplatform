using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Shared.Controllers.PushNotification;

namespace Boilerplate.Client.Core.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    public virtual string Token { get; set; }
    public virtual Task<bool> IsPushNotificationSupported(CancellationToken cancellationToken) => Task.FromResult(false);
    public abstract Task<PushNotificationSubscriptionDto> GetSubscription(CancellationToken cancellationToken);

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        if (await IsPushNotificationSupported(cancellationToken) is false)
        {
            Logger.LogWarning("Notifications are not supported/allowed on this platform/device.");
            return;
        }

        var subscription = await GetSubscription(cancellationToken);

        if (subscription is null)
        {
            Logger.LogWarning("Could not retrieve push notification subscription"); // Browser's incognito mode etc.
            return;
        }

        await pushNotificationController.Subscribe(subscription, cancellationToken);
    }

    public async Task Unsubscribe(string deviceId, CancellationToken cancellationToken)
    {
        await pushNotificationController.Unsubscribe(deviceId, cancellationToken);
    }
}
