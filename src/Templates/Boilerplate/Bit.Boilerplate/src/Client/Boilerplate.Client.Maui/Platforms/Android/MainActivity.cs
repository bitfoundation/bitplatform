using Android.OS;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Boilerplate.Client.Core;
using Java.Net;

namespace Boilerplate.Client.Maui.Platforms.Android;

[IntentFilter([Intent.ActionView],
                        DataSchemes = ["https", "http"],
                        DataHosts = ["bp.bitplatform.dev"],
                        DataPaths = ["/"],
                        DataPathPrefixes = ["/add-edit-category", "/categories", "/dashboard", "/confirm", "/forgot-password",
                            "/profile", "/reset-password", "/sign-in", "/sign-up", "/not-authorized", "/not-found",
                            "/offline-edit-profile", "/products", "/terms", "/todo", "/about", "/about"],
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
            var _ = Routes.OpenUniversalLink(new URL(url).File ?? "/");
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        var action = intent!.Action;
        var url = intent.DataString;
        if (action is Intent.ActionView && string.IsNullOrWhiteSpace(url) is false)
        {
            var _ = Routes.OpenUniversalLink(new URL(url).File ?? "/");
        }
    }
}
