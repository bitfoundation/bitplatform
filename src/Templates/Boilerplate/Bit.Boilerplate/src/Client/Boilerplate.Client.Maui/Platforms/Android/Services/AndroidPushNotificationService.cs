using Android.Gms.Common;
using Firebase.Messaging;
using Plugin.LocalNotification;
using static Android.Provider.Settings;
using Boilerplate.Client.Core.Components;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.Android.Services;

public partial class AndroidPushNotificationService : PushNotificationServiceBase
{
    public override async Task<bool> IsAvailable(CancellationToken cancellationToken)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            return await LocalNotificationCenter.Current.AreNotificationsEnabled()
                && GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) == ConnectionResult.Success;
        });
    }

    public override async Task RequestPermission(CancellationToken cancellationToken)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (await IsAvailable(cancellationToken) is false
                && await LocalNotificationCenter.Current.RequestNotificationPermission())
            {
                Configure();
            }
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
    public static void Configure()
    {
        FirebaseMessaging.Instance.GetToken().AddOnSuccessListener((MainActivity)Platform.CurrentActivity!);
        LocalNotificationCenter.Current.NotificationActionTapped += (e) =>
        {
            if (string.IsNullOrEmpty(e.Request.ReturningData))
                return;
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(e.Request.ReturningData)!;
            if (data.TryGetValue("pageUrl", out var pageUrl))
            {
                _ = Routes.OpenUniversalLink(pageUrl ?? Urls.HomePage);
            }
        };
    }
}
