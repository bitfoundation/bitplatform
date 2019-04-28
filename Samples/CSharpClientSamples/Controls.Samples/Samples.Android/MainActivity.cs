using Android.App;
using Android.Content.PM;
using Android.OS;
using Bit.View;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Bit.CSharpClient.Controls.Samples.Droid
{
    [Activity(Label = "Bit.CSharpClient.Controls.Samples", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            BitCSharpClientControls.Init();
            Forms.Init(this, savedInstanceState);

            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);

            LoadApplication(new App());
        }
    }
}
