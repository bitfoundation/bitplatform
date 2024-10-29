using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsPushNotificationService : PushNotificationServiceBase
{
    public override Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
