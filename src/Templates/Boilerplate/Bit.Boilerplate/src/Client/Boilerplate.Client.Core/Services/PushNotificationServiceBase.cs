using System.Text.Json;
using Boilerplate.Shared.Controllers;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services;

public partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;
    [AutoInject] protected IStorageService storageService = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public virtual string Token { get; set; }
    public virtual bool NotificationsSupported => false;
    public virtual string GetDeviceId() => throw new NotImplementedException();
    public virtual DeviceInstallationDto GetDeviceInstallation() => throw new NotImplementedException();

    public async Task RegisterDeviceAsync(CancellationToken cancellationToken)
    {
        if (NotificationsSupported is false)
            return;

        var deviceInstallation = GetDeviceInstallation();

        await pushNotificationController.CreateOrUpdateInstallation(deviceInstallation, cancellationToken);

        await storageService.SetItem("device_token", deviceInstallation.PushChannel);
    }

    public async Task DeregisterDeviceAsync(CancellationToken cancellationToken)
    {
        if (NotificationsSupported is false)
            return;

        var cachedToken = await storageService.GetItem("device_token");

        if (cachedToken == null)
            return;

        var deviceId = GetDeviceId() ?? throw new InvalidOperationException("Unable to resolve an ID for the device.");

        await pushNotificationController.DeleteInstallation(deviceId, cancellationToken);

        await storageService.RemoveItem("device_token");
    }
}
