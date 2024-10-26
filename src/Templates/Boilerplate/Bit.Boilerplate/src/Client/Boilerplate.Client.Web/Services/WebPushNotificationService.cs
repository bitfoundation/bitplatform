using Boilerplate.Client.Core;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Web.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private readonly IJSRuntime jSRuntime = default!;
    [AutoInject] private readonly WebAppRenderMode webAppRenderMode = default!;
    [AutoInject] private readonly ClientAppSettings clientAppSettings = default!;

    public async override Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        return await jSRuntime.GetDeviceInstallation(clientAppSettings.AdsPushVapid!.PublicKey);
    }

    public override async Task<bool> IsNotificationSupported() => webAppRenderMode.PwaEnabled
        && string.IsNullOrEmpty(clientAppSettings.AdsPushVapid?.PublicKey) is false;
}
