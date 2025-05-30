using Firebase.Messaging;
using Plugin.LocalNotification;
using Microsoft.Extensions.Logging;
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
            return LocalNotificationCenter.Current.IsSupported
                && await LocalNotificationCenter.Current.AreNotificationsEnabled();
        });
    }

    public override async Task RequestPermission(CancellationToken cancellationToken)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (LocalNotificationCenter.Current.IsSupported is false)
                return;

            await LocalNotificationCenter.Current.RequestNotificationPermission();
            Configure();
        });
    }

    public string GetDeviceId() => Secure.GetString(Platform.AppContext.ContentResolver, Secure.AndroidId)!;

    public override async Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken)
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
            Logger.LogError(exp, "Unable to resolve token for FCMv1.");
            return null;
        }

        var subscription = new PushNotificationSubscriptionDto
        {
            DeviceId = GetDeviceId(),
            Platform = "fcmV1",
            PushChannel = Token
        };

        return subscription;
    }

    private static bool _isConfigured = false;
    public static void Configure()
    {
        if (_isConfigured)
            return;
        _isConfigured = true;
        FirebaseMessaging.Instance.GetToken().AddOnSuccessListener((MainActivity)Platform.CurrentActivity!);
    }
}
