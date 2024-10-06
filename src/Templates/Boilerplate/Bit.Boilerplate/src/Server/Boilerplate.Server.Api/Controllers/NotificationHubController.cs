//-:cnd:noEmit
using Boilerplate.Shared.Controllers;
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Server.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class NotificationHubController : AppControllerBase, INotificationHubController
{
    [AutoInject] AzureNotificationHubService pushNotificationService = default!;

    [HttpPost]
    public async Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken)
    {
        await pushNotificationService.CreateOrUpdateInstallation(deviceInstallation, cancellationToken);
    }

#if Development // This action is for testing purposes only.
    [HttpPost]
    public async Task RequestPush([FromQuery] string? title = null, [FromQuery] string? message = null, [FromQuery] string? action = null, [FromQuery(Name = "tags[]")] string[]? tags = null, [FromQuery] bool silent = false, CancellationToken cancellationToken = default)
    {
        await pushNotificationService.RequestPush(title, message, action, tags, silent, cancellationToken);
    }
#endif
}
