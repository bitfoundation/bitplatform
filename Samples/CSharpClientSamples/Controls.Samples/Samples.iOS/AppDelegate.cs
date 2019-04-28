using Bit.View;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace Bit.CSharpClient.Controls.Samples.iOS
{
    [Register(nameof(AppDelegate))]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            BitCSharpClientControls.Init();

            Forms.Init();

            Rg.Plugins.Popup.Popup.Init();

            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }
    }
}
