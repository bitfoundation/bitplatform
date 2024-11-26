//+:cnd:noEmit
using Foundation;
using UIKit;
//#if (notification == true)
using UserNotifications;
using Boilerplate.Client.Maui.Platforms.MacCatalyst.Services;
//#endif

namespace Boilerplate.Client.Maui.Platforms.MacCatalyst;

[Register(nameof(AppDelegate))]
public partial class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    //#if (notification == true)
    private IPushNotificationService NotificationService => IPlatformApplication.Current!.Services.GetRequiredService<IPushNotificationService>();

    [Export("application:didFinishLaunchingWithOptions:")]
    //#endif
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        //#if (notification == true)
        NotificationService.IsPushNotificationSupported(default).ContinueWith(task =>
        {
            if (task.Result)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.RegisterForRemoteNotifications();
                    UNUserNotificationCenter.Current.Delegate = new AppUNUserNotificationCenterDelegate();
                });
            }
        });

        // Use the following code the get the action value from the push notification when the app is launched by tapping on the push notification.
        // using var userInfo = launchOptions?.ObjectForKey(UIApplication.LaunchOptionsRemoteNotificationKey) as NSDictionary;
        // var actionValue = userInfo?.ObjectForKey(new NSString("action")) as NSString;
        //#endif

        return base.FinishedLaunching(application, launchOptions!);
    }

    //#if (notification == true)
    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public async void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        try
        {
            NotificationService.Token = deviceToken.ToHexString()!;
            await NotificationService.Subscribe(default);
        }
        catch (Exception exp)
        {
            IPlatformApplication.Current!.Services.GetRequiredService<IExceptionHandler>().Handle(exp);
        }
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        IPlatformApplication.Current!.Services.GetRequiredService<IExceptionHandler>().Handle(new InvalidOperationException(error.Description.ToString()));
    }
    //#endif
}
