using Boilerplate.Shared.Controllers.PushNotification;
using Boilerplate.Shared.Dtos.PushNotification;
using Microsoft.JSInterop;

namespace Boilerplate.Client.Core.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;
    [AutoInject] protected IConfiguration configuration = default!;
    [AutoInject] protected IJSRuntime jsRuntime = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;
    [AutoInject] protected ClientAppSettings ClientAppSettings = default!;

    public virtual string Token { get; set; }
    public virtual bool NotificationsSupported => false;
    public virtual async Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        return await jsRuntime.GetDeviceInstallation(ClientAppSettings.AdsPushVapid!.PublicKey!);
    }

    public async Task RegisterDevice(CancellationToken cancellationToken)
    {
        if (NotificationsSupported is false)
            return;

        var deviceInstallation = await GetDeviceInstallation();

        if (deviceInstallation is null)
            return;

        await pushNotificationController.RegisterDevice(deviceInstallation, cancellationToken);
    }

    public async Task DeregisterDevice(string deviceInstallationId, CancellationToken cancellationToken)
    {
        await pushNotificationController.DeregisterDevice(deviceInstallationId, cancellationToken);
    }
}
