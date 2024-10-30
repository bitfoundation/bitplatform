using AdminPanel.Server.Api.Services;
using AdminPanel.Shared.Dtos.PushNotification;
using AdminPanel.Shared.Controllers.PushNotification;

namespace AdminPanel.Server.Api.Controllers.PushNotification;

[Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class PushNotificationController : AppControllerBase, IPushNotificationController
{
    [AutoInject] PushNotificationService pushNotificationService = default!;

    [HttpPost]
    public async Task RegisterDevice([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken)
    {
        await pushNotificationService.RegisterDevice(deviceInstallation, cancellationToken);
    }

    [HttpPost("{deviceId}")]
    public async Task DeregisterDevice([Required] string deviceId, CancellationToken cancellationToken)
    {
        await pushNotificationService.DeregisterDevice(deviceId, cancellationToken);
    }

#if Development // This action is for testing purposes only.
    [HttpPost]
    public async Task RequestPush([FromQuery] string? title = null, [FromQuery] string? message = null, [FromQuery] string? action = null, CancellationToken cancellationToken = default)
    {
        await pushNotificationService.RequestPush(title, message, action, null, cancellationToken);
    }
#endif
}
