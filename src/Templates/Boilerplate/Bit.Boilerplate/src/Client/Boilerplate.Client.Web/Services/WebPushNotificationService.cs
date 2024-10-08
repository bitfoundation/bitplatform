using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Web.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IConfiguration configuration = default!;

    public override bool NotificationsSupported => AppRenderMode.PwaEnabled;

    public override async Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        return await jsRuntime.GetDeviceInstallation(configuration.GetRequiredValue<string>("VapidPublicKey"));
    }
}
