using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    public virtual string Token { get; set; }
    public virtual Task<bool> IsAvailable(CancellationToken cancellationToken) => Task.FromResult(false);
    public abstract Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken);
    public abstract Task RequestPermission(CancellationToken cancellationToken);

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        if (await IsAvailable(cancellationToken) is false)
        {
            Logger.LogWarning("Notifications are not supported/allowed on this platform/device.");
            return;
        }

        var subscription = await GetSubscription(cancellationToken);

        if (subscription is null)
            return;

        await pushNotificationController.Subscribe(subscription, cancellationToken);
    }
}
