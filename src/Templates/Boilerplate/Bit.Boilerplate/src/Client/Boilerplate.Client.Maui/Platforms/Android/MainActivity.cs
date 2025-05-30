//+:cnd:noEmit
using Java.Net;
using Android.OS;
using Android.App;
using Android.Content;
using Android.Content.PM;
//#if (notification == true)
using Android.Gms.Tasks;
using Plugin.LocalNotification;
//#endif
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Maui.Platforms.Android;

[IntentFilter([Intent.ActionView],
                        DataSchemes = ["https", "http"],
                        DataHosts = ["use-your-web-app-url-here.com"],
                        // the following app links will be opened in app instead of browser if the app is installed on Android device.
                        DataPaths = [Urls.HomePage],
                        DataPathPrefixes = [
                            "/en-US", "/en-GB", "/nl-NL", "/fa-IR", "sv-SE", "hi-IN", "zh-CN", "es-ES", "fr-FR", "ar-SA", "de-DE",
                            Urls.ConfirmPage, Urls.ForgotPasswordPage, Urls.SettingsPage, Urls.ResetPasswordPage, Urls.SignInPage,
                            Urls.SignUpPage, Urls.NotAuthorizedPage, Urls.NotFoundPage, Urls.TermsPage, Urls.AboutPage, Urls.Authorize, Urls.AboutPage,
                            //#if (module == "Admin")
                            Urls.AddOrEditProductPage, Urls.CategoriesPage, Urls.DashboardPage, Urls.ProductsPage,
                            //#endif
                            //#if (module == "Sales")
                            Urls.ProductPage,
                            //#endif
                            //#if (sample == true)
                            Urls.TodoPage,
                            //#endif
                            //#if (offlineDb == true)
                            Urls.OfflineDatabaseDemo
                            //#endif
                            ],
                        AutoVerify = true,
                        Categories = [Intent.ActionView, Intent.CategoryDefault, Intent.CategoryBrowsable])]

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleInstance,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public partial class MainActivity : MauiAppCompatActivity
    //#if (notification == true)
    , IOnSuccessListener
//#endif
{
    //#if (notification == true)
    private IPushNotificationService PushNotificationService => IPlatformApplication.Current!.Services.GetRequiredService<IPushNotificationService>();
    //#endif

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // https://github.com/dotnet/maui/issues/24742
        Theme?.ApplyStyle(Resource.Style.OptOutEdgeToEdgeEnforcement, force: false);

        base.OnCreate(savedInstanceState);

        var url = Intent?.DataString; // Handling universal deep links handling when the app was closed.
        if (string.IsNullOrWhiteSpace(url) is false)
        {
            _ = Routes.OpenUniversalLink(new URL(url).File ?? Urls.HomePage);
        }

        //#if (notification == true)
        HandlePushNotificationTap(); // Handling push notification taps when the app was closed.
        PushNotificationService.IsAvailable(default).ContinueWith(task =>
        {
            if (task.Result)
            {
                Services.AndroidPushNotificationService.Configure();
            }
        });
        //#endif
    }

    //#if (notification == true)
    private void HandlePushNotificationTap()
    {
        var dataString = Intent?.GetStringExtra(LocalNotificationCenter.ReturnRequest);
        string? pageUrl = null;
        if (string.IsNullOrEmpty(dataString) is false)
        {
            var request = JsonSerializer.Deserialize<NotificationRequest>(dataString, options: new()
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            });
            if (request?.ReturningData is not null)
            {
                var returningData = JsonSerializer.Deserialize<Dictionary<string, object>>(request.ReturningData);
                if (returningData?.ContainsKey("pageUrl") is true)
                {
                    pageUrl = returningData["pageUrl"]?.ToString(); // The time that the notification received, the app was open. (See PushNotificationFirebaseMessagingService's OnMessageReceived)
                }
            }
        }
        pageUrl ??= Intent?.GetStringExtra("pageUrl");
        if (string.IsNullOrEmpty(pageUrl) is false)
        {
            _ = Routes.OpenUniversalLink(pageUrl ?? Urls.HomePage); // The time that the notification received, the app was closed.
        }
    }
    //#endif

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        var action = intent!.Action; // Handling universal deep links handling when the is running.
        var url = intent.DataString;
        if (action is Intent.ActionView && string.IsNullOrWhiteSpace(url) is false)
        {
            _ = Routes.OpenUniversalLink(new URL(url).File ?? Urls.HomePage);
        }

        //#if (notification == true)
        HandlePushNotificationTap(); // Handling push notification taps when the app is running.
        //#endif
    }

    //#if (notification == true)
    public void OnSuccess(Java.Lang.Object? result)
    {
        PushNotificationService.Token = result!.ToString();
    }
    //#endif
}
