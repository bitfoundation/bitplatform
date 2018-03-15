using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Bit.Droid;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Prism;
using Prism.Ioc;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Bit.CSharpClientSample.Droid
{
    [Activity(Label = "Bit.CSharpClientSample", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            LoadApplication(new App(new TestAppInitializer(this)));
        }
    }

    public class TestAppInitializer : IPlatformInitializer
    {
        private readonly Activity _activity;

        public TestAppInitializer(Activity activity)
        {
            _activity = activity;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance<Activity>(_activity);
            containerRegistry.RegisterInstance<Context>(_activity);

            containerRegistry.Register<IBrowserService, DefaultBrowserService>();
        }
    }

    [Activity(Label = nameof(TestAppSSOUrlRedirectParserActivity), NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataSchemes = new[] { "test" },
    DataPath = "test://oauth2redirect")]
    public class TestAppSSOUrlRedirectParserActivity : BitSSOUrlRedirectParserActivity
    {
    }
}

