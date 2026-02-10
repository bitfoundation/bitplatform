namespace Boilerplate.Shared.Features.PushNotification;

[Route("api/v1/[controller]/[action]/")]
public interface IPushNotificationController : IAppController
{
    [HttpPost]
    Task Subscribe([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken);
}
