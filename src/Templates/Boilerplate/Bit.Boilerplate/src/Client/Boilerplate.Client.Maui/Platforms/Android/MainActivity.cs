//+:cnd:noEmit
using Android.OS;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Boilerplate.Client.Core;
using Java.Net;

namespace Boilerplate.Client.Maui.Platforms.Android;

[IntentFilter([Intent.ActionView],
                        DataSchemes = ["https", "http"],
                        DataHosts = ["use-your-server-url-here.com"],
                        // the following app links will be opened in app instead of browser if the app is installed on Android device.
                        DataPaths = ["/"],
                        DataPathPrefixes = [
                            "/confirm", "/forgot-password","/profile", "/reset-password", "/sign-in", "/sign-up", "/not-authorized", "/not-found","/terms", "/about",
                            //#if (sample == "Admin")
                            "/add-edit-category", "/categories", "/dashboard", "/products",
                            //#elif (sample == "Todo")
                            "/todo",
                            //#endif
                            //#if (offlineDb == true)
                            "/offline-edit-profile"
                            //#endif
                            ],
                        AutoVerify = true,
                        Categories = [Intent.ActionView, Intent.CategoryDefault, Intent.CategoryBrowsable])]

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleInstance,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var url = Intent?.DataString;
        if (string.IsNullOrWhiteSpace(url) is false)
        {
            _ = Routes.OpenUniversalLink(new URL(url).File ?? "/");
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        var action = intent!.Action;
        var url = intent.DataString;
        if (action is Intent.ActionView && string.IsNullOrWhiteSpace(url) is false)
        {
            _ = Routes.OpenUniversalLink(new URL(url).File ?? "/");
        }
    }
}
