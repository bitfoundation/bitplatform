using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Bit.Android;
using Bit.ViewModel.Implementations;
using Xamarin.Forms;

namespace Bit.CSharpClientSample.Droid
{
    [Activity(Label = "Bit.CSharpClientSample", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : BitFormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            SQLitePCL.Batteries.Init();

            base.OnCreate(savedInstanceState);

            UseDefaultConfiguration(savedInstanceState);
            Forms.Init(this, savedInstanceState);

            LoadApplication(new App(new SampleAppDroidInitializer(this)));
        }
    }

    public class SampleAppDroidInitializer : BitPlatformInitializer
    {
        public SampleAppDroidInitializer(Activity activity)
            : base(activity)
        {
        }
    }

    [Activity(Label = nameof(SampleAppSSOUrlRedirectParserActivity), NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataSchemes = new[] { "test" },
    DataPath = "test://oauth2redirect")]
    public class SampleAppSSOUrlRedirectParserActivity : BitSSOUrlRedirectParserActivity
    {
    }
}
