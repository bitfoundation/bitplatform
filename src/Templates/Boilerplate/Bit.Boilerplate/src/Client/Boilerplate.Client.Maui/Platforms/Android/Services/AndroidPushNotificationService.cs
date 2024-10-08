using Android.Gms.Common;
using static Android.Provider.Settings;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.Android.Services;

public partial class AndroidPushNotificationService : PushNotificationServiceBase
{
    public override bool NotificationsSupported => GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext) == ConnectionResult.Success;

    public override string GetDeviceId() => Secure.GetString(Platform.AppContext.ContentResolver, Secure.AndroidId)!;

    public override async Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        if (!NotificationsSupported)
            throw new InvalidOperationException(GetPlayServicesError());

        if (string.IsNullOrWhiteSpace(Token))
            throw new InvalidOperationException("Unable to resolve token for FCMv1.");

        var installation = new DeviceInstallationDto
        {
            InstallationId = GetDeviceId(),
            Platform = "fcmV1",
            PushChannel = Token
        };

        return installation;
    }

    private string GetPlayServicesError()
    {
        int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext);

        if (resultCode != ConnectionResult.Success)
            return GoogleApiAvailability.Instance.IsUserResolvableError(resultCode) ?
                       GoogleApiAvailability.Instance.GetErrorString(resultCode) :
                       "This device isn't supported.";

        return "An error occurred preventing the use of push notifications.";
    }
}
