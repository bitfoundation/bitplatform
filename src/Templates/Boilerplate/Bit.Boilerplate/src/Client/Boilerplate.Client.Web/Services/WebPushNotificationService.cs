using Boilerplate.Client.Core;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Web.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private IJSRuntime jSRuntime = default!;
    [AutoInject] private ClientAppSettings clientAppSettings = default!;

    public async override Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        return await jSRuntime.GetDeviceInstallation(clientAppSettings.AdsPushVapid!.PublicKey);
    }

    public override async Task<bool> IsNotificationSupported() => AppRenderMode.PwaEnabled
        && string.IsNullOrEmpty(clientAppSettings.AdsPushVapid?.PublicKey) is false;
}
