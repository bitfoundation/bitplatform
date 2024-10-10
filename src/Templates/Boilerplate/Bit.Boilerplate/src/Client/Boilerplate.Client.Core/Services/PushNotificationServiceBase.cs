using Boilerplate.Shared.Controllers.PushNotification;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;
    [AutoInject] protected IConfiguration configuration = default!;
    [AutoInject] protected IJSRuntime jsRuntime = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public virtual string Token { get; set; }
    public virtual bool NotificationsSupported => false;
    public virtual async Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        return await jsRuntime.GetDeviceInstallation(configuration.GetRequiredValue<string>("AdsPush:Primary:Vapid:PublicKey"));
    }

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
