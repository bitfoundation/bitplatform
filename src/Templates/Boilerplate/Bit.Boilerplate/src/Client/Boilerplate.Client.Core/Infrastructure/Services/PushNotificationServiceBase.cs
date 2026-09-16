using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;
    [AutoInject] protected NotificationPreferenceService notificationPreferenceService = default!;

    /// <summary>
    /// Orders <see cref="Subscribe"/> and <see cref="Unsubscribe"/> against each other. AppMenu stores the device's
    /// choice before calling either, so the automatic subscribe on every auth-state change (See AppClientCoordinator)
    /// either reads the new choice or, already past the check, finishes before the unsubscribe removes its row.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);

    public virtual string? Token { get; set; }
    public virtual Task<bool> IsAvailable(CancellationToken cancellationToken) => Task.FromResult(false);
    public abstract Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken);
    public abstract Task RequestPermission(CancellationToken cancellationToken);

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (await notificationPreferenceService.IsEnabled() is false)
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

    protected virtual async Task SubscribeCore(CancellationToken cancellationToken)
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
