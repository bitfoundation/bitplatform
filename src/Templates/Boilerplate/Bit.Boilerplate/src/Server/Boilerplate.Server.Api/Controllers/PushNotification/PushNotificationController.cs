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
        if (User.IsAuthenticated() is false && Request.Headers.Authorization.Any() is true)
        {
            // PushNotificationController allows anonymous notification subscriptions. However, if an Authorization is included
            // and the user is not authenticated, it indicates the client has sent an invalid or expired access token.
            // In this scenario, we should refresh the access token and attempt to re-send the api request.
            throw new UnauthorizedException();
        }

        await pushNotificationService.Subscribe(subscription, cancellationToken);
    }
}
