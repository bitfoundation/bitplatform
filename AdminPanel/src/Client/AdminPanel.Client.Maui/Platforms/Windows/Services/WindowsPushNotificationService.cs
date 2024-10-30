using AdminPanel.Shared.Dtos.PushNotification;

namespace AdminPanel.Client.Maui.Platforms.Windows.Services;

public partial class WindowsPushNotificationService : PushNotificationServiceBase
{
    public override Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken) => 
        throw new NotImplementedException();

}
