using Boilerplate.Shared.Controllers;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected INotificationHubController pushNotificationController = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public virtual string Token { get; set; }
    public virtual bool NotificationsSupported => false;
    public virtual string GetDeviceId() => throw new NotImplementedException();
    public virtual Task<DeviceInstallationDto> GetDeviceInstallation() => throw new NotImplementedException();

    public async Task RegisterDevice(CancellationToken cancellationToken)
    {
        if (NotificationsSupported is false)
            return;

        var deviceInstallation = await GetDeviceInstallation();

        if (deviceInstallation is null)
            return;

        await pushNotificationController.CreateOrUpdateInstallation(deviceInstallation, cancellationToken);
    }
}
