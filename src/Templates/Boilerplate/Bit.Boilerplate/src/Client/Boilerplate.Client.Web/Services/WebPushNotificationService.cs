using Bit.Butil;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Web.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private Notification notification = default!;
    [AutoInject] private readonly IJSRuntime jSRuntime = default!;
    [AutoInject] private readonly ClientWebSettings clientWebSettings = default!;

    public override async Task<PushNotificationSubscriptionDto> GetSubscription(CancellationToken cancellationToken)
    {
        return await jSRuntime.GetPushNotificationSubscription(clientWebSettings.AdsPushVapid!.PublicKey);
    }

    public override async Task<bool> IsAvailable(CancellationToken cancellationToken) => string.IsNullOrEmpty(clientWebSettings.AdsPushVapid?.PublicKey) is false && await notification.IsNotificationAvailable();

    public override async Task RequestPermission(CancellationToken cancellationToken)
    {
        if (await IsAvailable(cancellationToken) is false)
        {
            await notification.RequestPermission();
        }
    }
}
