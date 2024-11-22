﻿//-:cnd:noEmit
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
    public async Task RegisterSubscription([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken)
    {
        await pushNotificationService.RegisterSubscription(subscription, cancellationToken);
    }

    [HttpPost("{deviceId}")]
    public async Task DeregisterSubscription([Required] string deviceId, CancellationToken cancellationToken)
    {
        await pushNotificationService.DeregisterSubscription(deviceId, cancellationToken);
    }

#if Development // This action is for testing purposes only.
    [HttpPost]
    public async Task RequestPush([FromQuery] string? title = null, [FromQuery] string? message = null, [FromQuery] string? action = null, CancellationToken cancellationToken = default)
    {
        await pushNotificationService.RequestPush(title, message, action, false, null, cancellationToken);
    }
#endif
}
