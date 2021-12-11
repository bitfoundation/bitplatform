using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Bit.Android;
using Bit.ViewModel.Implementations;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Internals.Preserve]

namespace Bit.CSharpClientSample.Droid6
{
    [Activity(Label = "Bit.CSharpClientSample6", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : BitFormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            LocalTelemetryService.Current.Init();
            AppCenterTelemetryService.Current.Init("79771c81-f748-4649-8787-74508df44145",
                   typeof(Analytics), typeof(Crashes));
            ApplicationInsightsTelemetryService.Current.Init("55f4c3a7-8bd1-4ec0-92b3-717cb9ddde1d");

            SQLitePCL.Batteries_V2.Init();

            base.OnCreate(savedInstanceState);

            UseDefaultConfiguration(savedInstanceState);
            Forms.SetFlags("StateTriggers_Experimental");
            Forms.Init(this, savedInstanceState);

            LoadApplication(new App(new SampleAppDroidInitializer(this)));
        }
    }

    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "test-oauth")]
    public class WebAuthenticationCallbackActivity : Microsoft.Maui.Essentials.WebAuthenticatorCallbackActivity
    {
    }

    public class SampleAppDroidInitializer : BitPlatformInitializer
    {
        public SampleAppDroidInitializer(Activity activity)
            : base(activity)
        {
        }
    }
}
