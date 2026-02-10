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
}
