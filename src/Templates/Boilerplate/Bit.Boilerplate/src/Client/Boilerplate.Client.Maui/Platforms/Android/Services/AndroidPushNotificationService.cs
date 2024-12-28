using Android.Gms.Common;
using Plugin.LocalNotification;
using static Android.Provider.Settings;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.Android.Services;

public partial class AndroidPushNotificationService : PushNotificationServiceBase
{
    public override async Task<bool> IsPushNotificationSupported(CancellationToken cancellationToken)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() is false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            return await LocalNotificationCenter.Current.AreNotificationsEnabled() &&
                GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) == ConnectionResult.Success;
        });
    }

    public string GetDeviceId() => Secure.GetString(Platform.AppContext.ContentResolver, Secure.AndroidId)!;

    public override async Task<PushNotificationSubscriptionDto> GetSubscription(CancellationToken cancellationToken)
    {
        try
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            while (string.IsNullOrEmpty(Token))
            {
                // After the NotificationsSupported Task completes with a result of true,
                // we use FirebaseMessaging.Instance.GetToken.
                // This method is asynchronous and we need to wait for it to complete.
                await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
            }
        }
        catch (Exception exp)
        {
            throw new InvalidOperationException("Unable to resolve token for FCMv1.", exp);
        }

        var subscription = new PushNotificationSubscriptionDto
        {
            DeviceId = GetDeviceId(),
            Platform = "fcmV1",
            PushChannel = Token
        };

        return subscription;
    }
}
