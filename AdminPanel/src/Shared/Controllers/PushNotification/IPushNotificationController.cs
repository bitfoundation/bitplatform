using AdminPanel.Shared.Dtos.PushNotification;

namespace AdminPanel.Shared.Controllers.PushNotification;

[Route("api/[controller]/[action]/")]
public interface IPushNotificationController : IAppController
{
    [HttpPost]
    Task RegisterDevice([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken);

    [HttpPost("{deviceId}")]
    Task DeregisterDevice([Required] string deviceId, CancellationToken cancellationToken);
}
