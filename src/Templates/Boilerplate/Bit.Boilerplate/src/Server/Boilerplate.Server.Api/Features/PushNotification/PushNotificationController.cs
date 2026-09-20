//-:cnd:noEmit
using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Server.Api.Features.PushNotification;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class PushNotificationController : AppControllerBase, IPushNotificationController
{
    [AutoInject] PushNotificationService pushNotificationService = default!;

    [HttpPost]
    public async Task Subscribe([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken)
    {
        HttpContext.ThrowIfContainsExpiredAccessToken();

        await pushNotificationService.Subscribe(subscription, cancellationToken);
    }

    [HttpPost]
    public async Task Unsubscribe([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken)
    {
        HttpContext.ThrowIfContainsExpiredAccessToken();

        await pushNotificationService.Unsubscribe(subscription.DeviceId!, cancellationToken);
    }

    [HttpPost]
    public async Task TestPushNotificationSetup([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken)
    {
        HttpContext.ThrowIfContainsExpiredAccessToken();

        // The same "DeviceId IS the device's credential" model as Subscribe (read the comment in PushNotificationService):
        // the push goes to that device and nowhere else, so presenting a DeviceId only ever notifies its own holder.
        await pushNotificationService.RequestPush(new()
        {
            Title = Localizer[nameof(AppStrings.TestPushNotificationTitle)],
            Message = Localizer[nameof(AppStrings.TestPushNotificationMessage)],
            PageUrl = PageUrls.PrivacyPolicy,
            UserRelatedPush = false
        }, s => s.DeviceId == subscription.DeviceId, cancellationToken);
    }
}
