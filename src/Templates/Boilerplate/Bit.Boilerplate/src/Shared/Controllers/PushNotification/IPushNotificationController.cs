using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Shared.Controllers.PushNotification;

[Route("api/[controller]/[action]/")]
public interface IPushNotificationController : IAppController
{
    [HttpPost]
    Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken);
}
