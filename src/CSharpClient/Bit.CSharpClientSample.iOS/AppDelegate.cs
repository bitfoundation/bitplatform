using Bit.iOS;
using Bit.ViewModel.Contracts;
using Bit.ViewModel.Implementations;
using Foundation;
using Prism;
using Prism.Ioc;
using UIKit;
using Xamarin.Forms;

namespace Bit.CSharpClientSample.iOS
{
    [Register(nameof(AppDelegate))]
    public partial class AppDelegate : BitFormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.Init();

            LoadApplication(new App(new SampleAppiOSInitializer()));

            return base.FinishedLaunching(app, options);
        }
    }

    public class SampleAppiOSInitializer : IPlatformInitializer
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IBrowserService, DefaultBrowserService>();
        }
    }
}
