//+:cnd:noEmit
using Android.OS;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Java.Net;
//#if (notification == true)
using Android.Gms.Tasks;
using Firebase.Messaging;
//#endif
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Maui.Platforms.Android;

[IntentFilter([Intent.ActionView],
                        DataSchemes = ["https", "http"],
                        DataHosts = ["use-your-web-app-url-here.com"],
                        // the following app links will be opened in app instead of browser if the app is installed on Android device.
                        DataPaths = [Urls.HomePage],
                        DataPathPrefixes = [
                            "/en-US", "/en-GB", "/fa-IR", "/nl-NL",
                            Urls.ConfirmPage, Urls.ForgotPasswordPage, Urls.SettingsPage, Urls.ResetPasswordPage, Urls.SignInPage, Urls.SignUpPage, Urls.NotAuthorizedPage, Urls.NotFoundPage, Urls.TermsPage, Urls.AboutPage,
                            //#if (module == "Admin")
                            Urls.AddOrEditCategoryPage, Urls.CategoriesPage, Urls.DashboardPage, Urls.ProductsPage,
                            //#endif
                            //#if (module == "Sales")
                            Urls.ProductPage,
                            //#endif
                            //#if (sample == true)
                            Urls.TodoPage,
                            //#endif
                            //#if (offlineDb == true)
                            Urls.OfflineEditProfilePage
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

        var url = Intent?.DataString;
        if (string.IsNullOrWhiteSpace(url) is false)
        {
            _ = Routes.OpenUniversalLink(new URL(url).File ?? Urls.HomePage);
        }
        //#if (notification == true)
        PushNotificationService.IsPushNotificationSupported(default).ContinueWith(task =>
        {
            if (task.Result)
            {
                FirebaseMessaging.Instance.GetToken().AddOnSuccessListener(this);
            }
        });
        //#endif
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        var action = intent!.Action;
        var url = intent.DataString;
        if (action is Intent.ActionView && string.IsNullOrWhiteSpace(url) is false)
        {
            _ = Routes.OpenUniversalLink(new URL(url).File ?? Urls.HomePage);
        }
    }

    //#if (notification == true)
    public void OnSuccess(Java.Lang.Object? result)
    {
        PushNotificationService.Token = result!.ToString();
    }
    //#endif
}
