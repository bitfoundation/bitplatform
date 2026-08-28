using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IStorageService StorageService = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    private const string PushNotificationsDisabledStoreKey = "PushNotificationsDisabled";

    public virtual string? Token { get; set; }
    public virtual Task<bool> IsAvailable(CancellationToken cancellationToken) => Task.FromResult(false);
    public abstract Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken);
    public abstract Task RequestPermission(CancellationToken cancellationToken);

    public async Task<bool> IsEnabled() => await StorageService.GetItem(PushNotificationsDisabledStoreKey) is not "true";

    public async Task SetEnabled(bool enabled, CancellationToken cancellationToken)
    {
        if (enabled)
        {
            await StorageService.RemoveItem(PushNotificationsDisabledStoreKey);
            await Subscribe(cancellationToken);
        }
        else
        {
            await StorageService.SetItem(PushNotificationsDisabledStoreKey, "true", persistent: true);
            await Unsubscribe(cancellationToken);
        }
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        if (await IsEnabled() is false)
            return; // The automatic subscribe on every auth-state change (See AppClientCoordinator) must not undo the user's opt-out.

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

    public virtual async Task Unsubscribe(CancellationToken cancellationToken)
    {
        if (await IsAvailable(cancellationToken) is false)
            return;

        var subscription = await GetSubscription(cancellationToken);

        if (subscription?.DeviceId is null)
            return;

        await pushNotificationController.Unsubscribe(subscription, cancellationToken);
    }
}
