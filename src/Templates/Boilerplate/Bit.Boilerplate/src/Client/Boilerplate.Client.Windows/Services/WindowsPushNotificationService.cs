namespace Boilerplate.Client.Windows.Services;

public partial class WindowsPushNotificationService : PushNotificationServiceBase
{
    public override bool NotificationsSupported => false;
}
