using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IStorageService StorageService = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    private const string PushNotificationsDisabledStoreKey = "PushNotificationsDisabled";

    /// <summary>
    /// Orders <see cref="Subscribe"/>, <see cref="Unsubscribe"/> and <see cref="SetEnabled"/> against each other.
    /// Without it, the automatic subscribe that runs on every auth-state change (See AppClientCoordinator) could pass
    /// the opt-out check, lose the race to a SetEnabled(false) that stores the opt-out and removes the server row,
    /// and then recreate that row.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);

    public virtual string? Token { get; set; }
    public virtual Task<bool> IsAvailable(CancellationToken cancellationToken) => Task.FromResult(false);
    public abstract Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken);
    public abstract Task RequestPermission(CancellationToken cancellationToken);

    public async Task<bool> IsEnabled() => await StorageService.GetItem(PushNotificationsDisabledStoreKey) is not "true";

    public async Task SetEnabled(bool enabled, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (enabled)
            {
                await StorageService.RemoveItem(PushNotificationsDisabledStoreKey);
                await SubscribeCore(cancellationToken);
            }
            else
            {
                await StorageService.SetItem(PushNotificationsDisabledStoreKey, "true", persistent: true);
                await UnsubscribeCore(cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Checked under the gate: a SetEnabled(false) ahead of this call has stored the opt-out by the time this
            // runs, so the automatic subscribe on every auth-state change (See AppClientCoordinator) cannot undo it.
            if (await IsEnabled() is false)
                return;

            await SubscribeCore(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task Unsubscribe(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await UnsubscribeCore(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task SubscribeCore(CancellationToken cancellationToken)
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

    /// <summary>
    /// The native platforms' <see cref="GetSubscription"/> is a pure token lookup, so this base implementation may
    /// use it to identify the server row. The web's is not - it CREATES a browser subscription when none exists -
    /// which is why WebPushNotificationService overrides this with a non-creating lookup.
    /// </summary>
    protected virtual async Task UnsubscribeCore(CancellationToken cancellationToken)
    {
        if (await IsAvailable(cancellationToken) is false)
            return;

        var subscription = await GetSubscription(cancellationToken);

        if (subscription?.DeviceId is null)
            return;

        await pushNotificationController.Unsubscribe(subscription, cancellationToken);
    }
}
