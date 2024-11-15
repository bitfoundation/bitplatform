using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Shared.Controllers.PushNotification;

[Route("api/[controller]/[action]/"), AnonymousApi]
public interface IPushNotificationController : IAppController
{
    [HttpPost]
    Task RegisterDevice([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken);

    [HttpPost("{deviceId}")]
    Task DeregisterDevice([Required] string deviceId, CancellationToken cancellationToken);
}
