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

    [HttpDelete("{installationId}")]
    public async Task DeleteInstallation([Required] string installationId, CancellationToken cancellationToken)
    {
        await pushNotificationService.DeleteInstallation(installationId, cancellationToken);
    }

#if Development // This action is for testing purposes only.
    [HttpPost]
    public async Task RequestPush([FromQuery] string? text = null, [FromQuery] string? action = null, [FromQuery(Name = "tags[]")] string[]? tags = null, [FromQuery] bool silent = false, CancellationToken cancellationToken = default)
    {
        await pushNotificationService.RequestPush(text, action, tags, silent, cancellationToken);
    }
#endif
}
