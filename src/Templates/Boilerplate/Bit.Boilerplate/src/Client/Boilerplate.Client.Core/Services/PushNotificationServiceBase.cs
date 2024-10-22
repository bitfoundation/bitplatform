using Boilerplate.Shared.Dtos.PushNotification;
using Boilerplate.Shared.Controllers.PushNotification;

namespace Boilerplate.Client.Core.Services;

public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    public virtual string Token { get; set; }
    public virtual Task<bool> NotificationsSupported() => Task.FromResult(false);
    public abstract Task<DeviceInstallationDto> GetDeviceInstallation();

    public async Task RegisterDevice(CancellationToken cancellationToken)
    {
        if (await NotificationsSupported() is false)
            return;

        while (string.IsNullOrEmpty(Token))
        {
            // After the NotificationsSupported Task completes with a result of true,
            // we use FirebaseMessaging.Instance.GetToken and UNUserNotificationCenter.Current.Delegate.
            // Those methods are asynchronous and we need to wait for them to complete.
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        var deviceInstallation = await GetDeviceInstallation();

        await pushNotificationController.RegisterDevice(deviceInstallation, cancellationToken);
    }

    public async Task DeregisterDevice(string deviceInstallationId, CancellationToken cancellationToken)
    {
        await pushNotificationController.DeregisterDevice(deviceInstallationId, cancellationToken);
    }
}
