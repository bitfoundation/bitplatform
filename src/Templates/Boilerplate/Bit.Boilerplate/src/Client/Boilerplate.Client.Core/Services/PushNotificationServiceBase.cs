using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Shared.Controllers.PushNotification;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    public virtual string Token { get; set; }
    public virtual Task<bool> IsNotificationSupported(CancellationToken cancellationToken) => Task.FromResult(false);
    public abstract Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken);

    public async Task RegisterDevice(CancellationToken cancellationToken)
    {
        if (await IsNotificationSupported(cancellationToken) is false)
        {
            Logger.LogInformation("Notifications are not supported/allowed on this device.");
            return;
        }

        var deviceInstallation = await GetDeviceInstallation(cancellationToken);

        if (deviceInstallation is null)
        {
            Logger.LogInformation("Could not retrieve device installation"); // Browser's incognito mode etc.
            return;
        }

        await pushNotificationController.RegisterDevice(deviceInstallation, cancellationToken);
    }

    public async Task DeregisterDevice(string deviceInstallationId, CancellationToken cancellationToken)
    {
        await pushNotificationController.DeregisterDevice(deviceInstallationId, cancellationToken);
    }
}
