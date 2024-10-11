using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IPushNotificationService
{
    string Token { get; set; }
    bool NotificationsSupported { get; }
    Task<DeviceInstallationDto> GetDeviceInstallation();
    Task RegisterDevice(CancellationToken cancellationToken);
}
