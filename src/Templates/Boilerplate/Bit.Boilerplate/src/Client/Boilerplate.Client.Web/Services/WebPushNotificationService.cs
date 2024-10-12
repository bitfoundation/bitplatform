namespace Boilerplate.Client.Web.Services;

public partial class WebPushNotificationService : PushNotificationServiceBase
{
    public override bool NotificationsSupported => AppRenderMode.PwaEnabled;
}
