//+:cnd:noEmit
using Java.Net;
using Android.OS;
using Android.App;
using Android.Content;
using Android.Content.PM;
//#if (notification == true)
using Android.Gms.Tasks;
using Plugin.LocalNotification.Core.Models;
//#endif
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Maui.Platforms.Android;

[IntentFilter([Intent.ActionView],
                        DataSchemes = ["https", "http"],
                        DataHosts = ["use-your-web-app-url-here.com"],
                        // the following app links will be opened in app instead of browser if the app is installed on Android device.
                        DataPaths = [PageUrls.Home],
                        DataPathPrefixes = [
                            "/en-US", "/en-GB", "/nl-NL", "/fa-IR", "/sv-SE", "/hi-IN", "/zh-CN", "/es-ES", "/fr-FR", "/ar-SA", "/de-DE",
                            "/en-us", "/en-gb", "/nl-nl", "/fa-ir", "/sv-se", "/hi-in", "/zh-cn", "/es-es", "/fr-fr", "/ar-sa", "/de-de",
                            PageUrls.Confirm, PageUrls.ForgotPassword, PageUrls.Settings, PageUrls.ResetPassword, PageUrls.SignIn,
                            PageUrls.SignUp, PageUrls.NotAuthorized, PageUrls.NotFound, PageUrls.Terms, PageUrls.PrivacyPolicy, PageUrls.About,
                            PageUrls.Roles, PageUrls.Users, 
                            //#if (multitenant == true)
                            PageUrls.ManageMyTenants, PageUrls.ManageAllTenants,
                            //#endif
                            //#if (module == "Admin")
                            PageUrls.AddOrEditProduct, PageUrls.Categories, PageUrls.Dashboard, PageUrls.Products,
                            //#endif
                            //#if (module == "Sales")
                            PageUrls.Product,
                            //#endif
                            //#if (offlineDb == true)
                            PageUrls.OfflineTodo,
                            //#elseif (sample == true)
                            PageUrls.Todo,
                            //#endif
                            //#if (signalR == true)
                            PageUrls.SystemPrompts,
                            //#endif
                            ],
                        AutoVerify = true,
                        Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable])]

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask,
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

        OpenDeepLink(Intent); // Handling universal deep links handling when the app was closed.

        //#if (notification == true)
        HandlePushNotificationTap(Intent); // Handling push notification taps when the app was closed.
        PushNotificationService.IsAvailable(default).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                MauiProgram.LogException(task.Exception, reportedBy: nameof(IPushNotificationService.IsAvailable));
                return;
            }

            if (task.Result)
            {
                Services.AndroidPushNotificationService.Configure();
            }
        }, TaskScheduler.Default);
        //#endif
    }

    /// <summary>
    /// The activity is exported (it is the launcher activity), so any app on the device can send it an explicit
    /// intent whose data is not an http(s) url at all. <c>new URL("myscheme://x")</c> throws MalformedURLException,
    /// and an unhandled throw inside <c>OnCreate</c> kills the process on launch, so the parse is guarded here.
    /// <c>Routes.OpenUniversalLink</c> validates the resulting path before navigating to it.
    /// </summary>
    private static void OpenDeepLink(Intent? intent)
    {
        var url = intent?.DataString;
        if (string.IsNullOrWhiteSpace(url))
            return;

        string? path;
        try
        {
            path = new URL(url).File;
        }
        catch (Exception exp)
        {
            MauiProgram.LogException(exp, reportedBy: nameof(OpenDeepLink));
            return;
        }

        _ = Routes.OpenUniversalLink(string.IsNullOrWhiteSpace(path) ? PageUrls.Home : path);
    }

    //#if (notification == true)
    private static void HandlePushNotificationTap(Intent? intent)
    {
        if (intent is null)
            return;

        var dataString = intent.GetStringExtra(RequestConstants.ReturnRequest);
        string? pageUrl = null;
        if (string.IsNullOrWhiteSpace(dataString) is false)
        {
            try
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
            catch (JsonException exp)
            {
                MauiProgram.LogException(exp, reportedBy: nameof(HandlePushNotificationTap));
            }
        }

        pageUrl ??= intent?.Extras?.Get("pageUrl")?.ToString();
        if (string.IsNullOrWhiteSpace(pageUrl) is false)
        {
            _ = Routes.OpenUniversalLink(pageUrl ?? PageUrls.Home); // The time that the notification received, the app was closed.
        }
    }
    //#endif

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent!.Action is Intent.ActionView) // Handling universal deep links handling when the is running.
        {
            OpenDeepLink(intent);
        }

        //#if (notification == true)
        HandlePushNotificationTap(intent); // Handling push notification taps when the app is running.
        //#endif
    }

    //#if (notification == true)
    public void OnSuccess(Java.Lang.Object? result)
    {
        PushNotificationService.Token = result!.ToString();
    }
    //#endif
}
