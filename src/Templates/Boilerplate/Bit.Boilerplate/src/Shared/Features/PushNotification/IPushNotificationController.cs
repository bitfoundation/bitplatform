namespace Boilerplate.Shared.Features.PushNotification;

[Route("api/v1/[controller]/[action]/")]
public interface IPushNotificationController : IAppController
{
    [HttpPost]
    Task Subscribe([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken);

    [HttpPost]
    Task Unsubscribe([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken);

    /// <summary>
    /// Sends the welcome push that proves the setup worked, to the device that just subscribed. It is the signed out
    /// half of <c>IUserController.SetNotificationEnabled</c>, which does the same for a session it can store the
    /// preference on - an anonymous subscription has no session to carry one.
    /// </summary>
    [HttpPost]
    Task TestPushNotificationSetup([Required] PushNotificationSubscriptionDto subscription, CancellationToken cancellationToken);
}
