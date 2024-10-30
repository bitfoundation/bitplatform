using AdminPanel.Shared.Dtos.PushNotification;

namespace AdminPanel.Client.Web.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private readonly IJSRuntime jSRuntime = default!;
    [AutoInject] private readonly ClientWebSettings clientWebSettings = default!;

    public async override Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken)
    {
        return await jSRuntime.GetDeviceInstallation(clientWebSettings.AdsPushVapid!.PublicKey);
    }

    public override async Task<bool> IsNotificationSupported(CancellationToken cancellationToken) => clientWebSettings.WebAppRender.PwaEnabled
        && string.IsNullOrEmpty(clientWebSettings.AdsPushVapid?.PublicKey) is false;
}
