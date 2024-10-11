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
    public async Task RequestPush([FromQuery] string? title = null, [FromQuery] string? message = null, [FromQuery] string? action = null, [FromQuery(Name = "tags[]")] string[]? tags = null, CancellationToken cancellationToken = default)
    {
        await pushNotificationService.RequestPush(title, message, action, tags, null, cancellationToken);
    }
#endif
}
