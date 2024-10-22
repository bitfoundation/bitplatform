using Android.Gms.Common;
using Plugin.LocalNotification;
using static Android.Provider.Settings;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.Android.Services;

public partial class AndroidPushNotificationService : PushNotificationServiceBase
{
    public async override Task<bool> IsNotificationSupported()
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

    public override async Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        var installation = new DeviceInstallationDto
        {
            InstallationId = GetDeviceId(),
            Platform = "fcmV1",
            PushChannel = Token
        };

        return installation;
    }
}
