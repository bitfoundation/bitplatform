using Android.App;
using Android.Content;
using Firebase.Messaging;
using Plugin.LocalNotification;

namespace Boilerplate.Client.Maui.Platforms.Android.Services;

[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public partial class PushNotificationFirebaseMessagingService : FirebaseMessagingService
{
    private IPushNotificationService PushNotificationService => IPlatformApplication.Current!.Services.GetRequiredService<IPushNotificationService>();

    public override async void OnNewToken(string token)
    {
        try
        {
            PushNotificationService.Token = token;

            await PushNotificationService.RegisterDeviceAsync(default);
        }
        catch (Exception exp)
        {
            IPlatformApplication.Current!.Services.GetRequiredService<IExceptionHandler>().Handle(exp);
        }
    }

    public override async void OnMessageReceived(RemoteMessage message)
    {
        try
        {
            base.OnMessageReceived(message);

            var services = IPlatformApplication.Current!.Services;
            var localizer = services.GetRequiredService<IStringLocalizer<AppStrings>>();
            var pubSubService = services.GetRequiredService<IPubSubService>();

            message.Data.TryGetValue("action", out var messageAction);
            var notification = message.GetNotification();
            var title = notification!.Title;
            var body = notification.Body;

            if (string.IsNullOrEmpty(title) is false)
            {
                await LocalNotificationCenter.Current.Show(new()
                {
                    Title = localizer[title],
                    Description = localizer[body!],
                    ReturningData = messageAction! // See App.xaml.cs's OnNotificationActionTapped
                });
            }
            else if (string.IsNullOrEmpty(messageAction) is false)
            {
                pubSubService.Publish(messageAction, null);
            }
        }
        catch (Exception exp)
        {
            IPlatformApplication.Current!.Services.GetRequiredService<IExceptionHandler>().Handle(exp);
        }
    }
}
