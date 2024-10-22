using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IPushNotificationService
{
    string Token { get; set; }
    Task<bool> NotificationsSupported();
    Task<DeviceInstallationDto> GetDeviceInstallation();
    Task RegisterDevice(CancellationToken cancellationToken);
}
