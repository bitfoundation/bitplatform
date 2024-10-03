//+:cnd:noEmit
using Java.Net;
using Android.OS;
using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Android.Content.PM;
using Firebase.Messaging;
using Boilerplate.Client.Core;

namespace Boilerplate.Client.Maui.Platforms.Android;

[IntentFilter([Intent.ActionView],
                        DataSchemes = ["https", "http"],
                        DataHosts = ["use-your-server-url-here.com"],
                        // the following app links will be opened in app instead of browser if the app is installed on Android device.
                        DataPaths = ["/"],
                        DataPathPrefixes = [
                            "/en-US", "en-GB", "/fa-IR", "fr-FR",
                            Urls.ConfirmPage, Urls.ForgotPasswordPage, Urls.ProfilePage, Urls.ResetPasswordPage, Urls.SignInPage, Urls.SignUpPage, Urls.NotAuthorizedPage, Urls.NotFoundPage, Urls.TermsPage, Urls.AboutPage,
                            //#if (sample == "Admin")
                            Urls.AddOrEditCategoryPage, Urls.CategoriesPage, Urls.DashboardPage, Urls.ProductsPage,
                            //#elif (sample == "Todo")
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
public partial class MainActivity : MauiAppCompatActivity, IOnSuccessListener
{
    private IPushNotificationService PushNotificationService => IPlatformApplication.Current!.Services.GetRequiredService<IPushNotificationService>();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var url = Intent?.DataString;
        if (string.IsNullOrWhiteSpace(url) is false)
        {
            _ = Routes.OpenUniversalLink(new URL(url).File ?? Urls.HomePage);
        }

        if (PushNotificationService.NotificationsSupported)
            FirebaseMessaging.Instance.GetToken().AddOnSuccessListener(this);
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

    public void OnSuccess(Java.Lang.Object? result)
    {
        PushNotificationService.Token = result!.ToString();
    }
}
