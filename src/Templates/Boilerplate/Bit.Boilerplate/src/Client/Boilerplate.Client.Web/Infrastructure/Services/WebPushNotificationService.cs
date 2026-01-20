using Bit.Butil;

namespace Boilerplate.Client.Web.Infrastructure.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private Notification notification = default!;
    [AutoInject] private readonly IJSRuntime jSRuntime = default!;
    [AutoInject] private readonly ClientWebSettings clientWebSettings = default!;

    public override async Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken)
    {
        var subscription = await jSRuntime.GetPushNotificationSubscription(clientWebSettings.AdsPushVapid!.PublicKey);

        if (subscription is null)
        {
            Logger.LogError("Could not retrieve push notification subscription"); // Browser's incognito mode etc.
        }

        return subscription;
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
