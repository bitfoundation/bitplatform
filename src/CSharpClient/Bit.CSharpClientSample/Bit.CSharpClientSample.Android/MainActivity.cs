using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Bit.ViewModel.Implementations;
using Prism;
using Prism.Ioc;
using System;
using Xamarin.Auth.Presenters.XamarinAndroid;
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

            AuthenticationConfiguration.Init(this, bundle);

            LoadApplication(new App(new TestAppInitializer(this)));
        }
    }

    public class TestAppInitializer : IPlatformInitializer
    {
        private readonly Context _context;

        public TestAppInitializer(Context context)
        {
            _context = context;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(_context);
        }
    }

    [Activity(Label = "CustomUrlSchemeInterceptorActivity", NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataSchemes = new[] { "test" },
    DataPath = "test://oauth2redirect")]
    public class CustomUrlSchemeInterceptorActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Uri uri = new Uri(Intent.Data.ToString());

            DefaultSecurityService.OAuthAuthenticator?.OnPageLoading(uri);

            Finish();
        }
    }
}

