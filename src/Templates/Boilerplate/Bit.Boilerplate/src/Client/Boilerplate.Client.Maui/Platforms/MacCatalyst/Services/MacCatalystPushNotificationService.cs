using UIKit;
using Plugin.LocalNotification;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.MacCatalyst.Services;

public partial class MacCatalystPushNotificationService : PushNotificationServiceBase
{
    public override async Task<bool> IsPushNotificationSupported(CancellationToken cancellationToken)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() is false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            return await LocalNotificationCenter.Current.AreNotificationsEnabled();
        });
    }

    public string GetDeviceId() => UIDevice.CurrentDevice.IdentifierForVendor.ToString();

    public override async Task<PushNotificationSubscriptionDto> GetSubscription(CancellationToken cancellationToken)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        try
        {
            while (string.IsNullOrEmpty(Token))
            {
                // After the NotificationsSupported Task completes with a result of true,
                // we use UNUserNotificationCenter.Current.Delegate.
                // This method is asynchronous and we need to wait for it to complete.
                await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
            }
        }
        catch (Exception exp)
        {
            throw new InvalidOperationException("Unable to resolve token for APNS.", exp);
        }

        var subscription = new PushNotificationSubscriptionDto
        {
            DeviceId = GetDeviceId(),
            Platform = "apns",
            PushChannel = Token
        };

        return subscription;
    }
}
