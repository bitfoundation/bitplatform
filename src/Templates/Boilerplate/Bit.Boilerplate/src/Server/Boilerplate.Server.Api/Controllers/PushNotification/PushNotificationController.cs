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
    public async Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken)
    {
        await pushNotificationService.CreateOrUpdateInstallation(deviceInstallation, cancellationToken);
    }

#if Development // This action is for testing purposes only.
    [HttpPost]
    public async Task RequestPush([FromQuery] string? title = null, [FromQuery] string? message = null, [FromQuery] string? action = null, [FromQuery(Name = "tags[]")] string[]? tags = null, CancellationToken cancellationToken = default)
    {
        await pushNotificationService.RequestPush(title, message, action, tags, cancellationToken);
    }
#endif
}
