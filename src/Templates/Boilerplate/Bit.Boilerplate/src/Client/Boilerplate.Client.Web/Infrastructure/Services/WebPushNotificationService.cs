using Bit.Butil;
using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Client.Web.Infrastructure.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private Push push = default!;
    [AutoInject] private Notification notification = default!;
    [AutoInject] private ServiceWorker serviceWorker = default!;
    [AutoInject] private readonly ClientWebSettings clientWebSettings = default!;

    public override async Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken)
    {
        await WaitForActiveServiceWorker(cancellationToken);

        var subscription = await push.GetSubscription();

        if (subscription.IsActive is false)
        {
            subscription = await push.Subscribe(clientWebSettings.AdsPushVapid!.PublicKey);
        }

        if (subscription.IsActive is false)
        {
            // Browser's incognito mode, a denied notification permission, or a service worker that is not
            // registered yet. Returning null skips this round; the next auth-state propagation tries again.
            Logger.LogError("Could not retrieve push notification subscription");
            return null;
        }

        return new()
        {
            DeviceId = $"{subscription.P256dh}-{subscription.Auth}",
            Platform = "browser",
            P256dh = subscription.P256dh,
            Auth = subscription.Auth,
            Endpoint = subscription.Endpoint
        };
    }

    /// <summary>
    /// A push subscription is owned by the service worker registration, and <see cref="Push"/> reads that registration
    /// through <c>navigator.serviceWorker.getRegistration()</c>, which answers immediately - on a first visit it
    /// answers before the worker has activated. Asking then reports "not subscribed" for a browser that is merely a
    /// moment early, and nothing retries until the auth state changes again.
    /// <para>
    /// This waits for an activated worker rather than using <c>navigator.serviceWorker.ready</c>, whose promise simply
    /// never settles when no worker is ever registered (pwa turned off, unsupported browser). Giving up after the
    /// timeout lets the caller report the genuine "no subscription" case instead of hanging on it.
    /// </para>
    /// </summary>
    private async Task WaitForActiveServiceWorker(CancellationToken cancellationToken)
    {
        if (await serviceWorker.IsSupported() is false) return;

        var giveUpAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTimeOffset.UtcNow < giveUpAt)
        {
            if ((await serviceWorker.GetRegistration()).ActiveState is "activated") return;

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }

    public override async Task<bool> IsAvailable(CancellationToken cancellationToken) => string.IsNullOrEmpty(clientWebSettings.AdsPushVapid?.PublicKey) is false && await notification.IsNotificationAvailable();

    public override async Task RequestPermission(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(clientWebSettings.AdsPushVapid?.PublicKey))
            return;

        if (await notification.IsSupported() is false)
            return;

        await notification.RequestPermission();
    }
}
