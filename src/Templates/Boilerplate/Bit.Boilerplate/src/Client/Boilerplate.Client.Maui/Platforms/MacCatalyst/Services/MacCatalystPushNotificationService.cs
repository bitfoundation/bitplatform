using UIKit;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Client.Maui.Platforms.MacCatalyst.Services;

public partial class MacCatalystPushNotificationService : PushNotificationServiceBase
{
    public override bool NotificationsSupported => true;

    public string GetDeviceId() => UIDevice.CurrentDevice.IdentifierForVendor.ToString();

    public override async Task<DeviceInstallationDto> GetDeviceInstallation()
    {
        if (!NotificationsSupported)
            throw new InvalidOperationException(GetPlayServicesError());

        if (string.IsNullOrWhiteSpace(Token))
            throw new InvalidOperationException("Unable to resolve token for APNS.");

        var installation = new DeviceInstallationDto
        {
            InstallationId = GetDeviceId(),
            Platform = "apns",
            PushChannel = Token
        };

        return installation;
    }

    private string GetPlayServicesError()
    {
        if (Token == null)
            return $"This app can support notifications but you must enable this in your settings.";

        return "An error occurred preventing the use of push notifications";
    }
}
