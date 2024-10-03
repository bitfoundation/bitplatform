using Boilerplate.Shared.Controllers;
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Server.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class PushNotificationController : AppControllerBase, IPushNotificationController
{
    [AutoInject] PushNotificationService pushNotificationService = default!;

    [HttpPost]
    public async Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken)
    {
        await pushNotificationService.CreateOrUpdateInstallation(deviceInstallation, cancellationToken);
    }

    [HttpDelete("{installationId}")]
    public async Task DeleteInstallation([Required] string installationId, CancellationToken cancellationToken)
    {
        await pushNotificationService.DeleteInstallation(installationId, cancellationToken);
    }

#if Development // This action is for testing purposes only within the swagger UI.
    [HttpPost]
    public async Task RequestPush([Required] NotificationRequestDto notificationRequest, CancellationToken cancellationToken)
    {
        await pushNotificationService.RequestPush(notificationRequest, cancellationToken);
    }
#endif
}
