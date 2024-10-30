using AdminPanel.Shared.Dtos.PushNotification;

namespace AdminPanel.Client.Core.Services.Contracts;

public interface IPushNotificationService
{
    string Token { get; set; }
    Task<bool> IsNotificationSupported(CancellationToken cancellationToken);
    Task<DeviceInstallationDto> GetDeviceInstallation(CancellationToken cancellationToken);
    Task RegisterDevice(CancellationToken cancellationToken);
}
