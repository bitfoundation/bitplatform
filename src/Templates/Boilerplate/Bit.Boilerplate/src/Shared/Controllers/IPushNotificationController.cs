using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Shared.Controllers;

[Route("api/[controller]/[action]/")]
public interface IPushNotificationController : IAppController
{
    [HttpPost]
    Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken);

    [HttpDelete("{installationId}")]
    Task DeleteInstallation([Required] string installationId, CancellationToken cancellationToken);

    [HttpPost]
    Task RequestPush([Required] NotificationRequestDto notificationRequest, CancellationToken cancellationToken);
}
