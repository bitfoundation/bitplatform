// [mirror] UNUserNotificationCenter delegate - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Platforms/iOS/Services/AppUNUserNotificationCenterDelegate.cs

using Foundation;
using UserNotifications;

namespace Boilerplate.Client.Maui.Platforms.MacCatalyst.Services;

public partial class AppUNUserNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        // Runs when user taps on push notification.
        // Use the following code to get the action value from the tapped push notification.
        // var actionValue = response.Notification.Request.Content.UserInfo.ObjectForKey(new NSString("action")) as NSString;
        try
        {
            var pageUrl = response.Notification.Request.Content.UserInfo.ObjectForKey(new NSString("pageUrl")) as NSString;
            if (pageUrl != null)
            {
                await Core.Components.Routes.OpenUniversalLink(pageUrl);
            }
        }
        catch (Exception exp)
        {
            MauiProgram.LogException(exp, "AppUNUserNotificationCenterDelegate.DidReceiveNotificationResponse");
        }
        finally
        {
            completionHandler();
        }
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
