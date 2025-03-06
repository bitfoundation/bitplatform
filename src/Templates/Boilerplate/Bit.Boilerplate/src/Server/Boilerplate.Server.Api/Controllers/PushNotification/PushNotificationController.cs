//-:cnd:noEmit
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Shared.Controllers.PushNotification;

namespace Boilerplate.Server.Api.Controllers.PushNotification;

[Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class PushNotificationController : AppControllerBase, IPushNotificationController
{
    [AutoInject] PushNotificationService pushNotificationService = default!;

    [HttpPost]
    public async Task Subscribe([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken)
    {
        await pushNotificationService.Subscribe(subscription, cancellationToken);
    }

    [HttpPost("{deviceId}")]
    public async Task Unsubscribe([Required] string deviceId, CancellationToken cancellationToken)
    {
        await pushNotificationService.Unsubscribe(deviceId, cancellationToken);
    }
}
