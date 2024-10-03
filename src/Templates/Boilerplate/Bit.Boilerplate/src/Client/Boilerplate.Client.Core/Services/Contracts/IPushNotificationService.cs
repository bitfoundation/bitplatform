using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IPushNotificationService
{
    string Token { get; set; }
    bool NotificationsSupported { get; }
    string GetDeviceId();
    DeviceInstallationDto GetDeviceInstallation();
    Task DeregisterDeviceAsync(CancellationToken cancellationToken);
    Task RegisterDeviceAsync(CancellationToken cancellationToken);
}
