using UserNotifications;

namespace Boilerplate.Client.Maui.Platforms.iOS.Services;
public partial class AppUNUserNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        // Runs when user taps on push notification.
        // Use the following code to get the action value from the tapped push notification.
        // var actionValue = response.Notification.Request.Content.UserInfo.ObjectForKey(new NSString("action")) as NSString;
    }

    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        // Displays the notification when the app is in the foreground.
        completionHandler(UNNotificationPresentationOptions.Alert |
            UNNotificationPresentationOptions.Badge |
            UNNotificationPresentationOptions.Sound);

        // Use the following code to get the action value from the push notification.
        // var actionValue = notification.Request.Content.UserInfo.ObjectForKey(new NSString("action")) as NSString;
    }
}
