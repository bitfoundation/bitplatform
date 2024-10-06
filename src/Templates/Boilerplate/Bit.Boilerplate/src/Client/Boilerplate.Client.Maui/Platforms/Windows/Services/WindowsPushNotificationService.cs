namespace Boilerplate.Client.Maui.Platforms.Windows.Services;

public partial class WindowsPushNotificationService : PushNotificationServiceBase
{
    public override bool NotificationsSupported => false;
}
