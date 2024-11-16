using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Shared.Controllers.PushNotification;

namespace Boilerplate.Client.Core.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    public virtual string Token { get; set; }
    public virtual Task<bool> IsPushNotificationSupported(CancellationToken cancellationToken) => Task.FromResult(false);
    public abstract Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken);

    public async Task RegisterDevice(CancellationToken cancellationToken)
    {
        if (await IsPushNotificationSupported(cancellationToken) is false)
        {
            Logger.LogWarning("Notifications are not supported/allowed on this platform/device.");
            return;
        }

        var deviceInstallation = await GetDeviceInstallation(cancellationToken);

        if (deviceInstallation is null)
        {
            Logger.LogWarning("Could not retrieve device installation"); // Browser's incognito mode etc.
            return;
        }

        await pushNotificationController.RegisterDevice(deviceInstallation, cancellationToken);
    }

    public async Task DeregisterDevice(string deviceInstallationId, CancellationToken cancellationToken)
    {
        await pushNotificationController.DeregisterDevice(deviceInstallationId, cancellationToken);
    }
}
