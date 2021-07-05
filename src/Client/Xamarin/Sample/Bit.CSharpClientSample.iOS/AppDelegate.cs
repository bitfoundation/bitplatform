using Bit.iOS;
using Bit.ViewModel.Implementations;
using Foundation;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using UIKit;
using Xamarin.Forms;

namespace Bit.CSharpClientSample.iOS
{
    [Register(nameof(AppDelegate))]
    public class AppDelegate : BitFormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // https://stackoverflow.com/a/51367084/2720104
            // <FirebaseCrashlyticsUploadSymbolsEnabled>True</FirebaseCrashlyticsUploadSymbolsEnabled>
            // <StartArguments>-FIRAnalyticsDebugEnabled</StartArguments>
            // <BundleResource Include="GoogleService-Info.plist" />
            Firebase.Core.App.Configure();
            Firebase.Analytics.Analytics.SetAnalyticsCollectionEnabled(true);
            Firebase.Crashlytics.Crashlytics.SharedInstance.Init();
            Firebase.Crashlytics.Crashlytics.SharedInstance.SetCrashlyticsCollectionEnabled(true);

            FirebaseTelemetryService.Current.Init();
            LocalTelemetryService.Current.Init();
            AppCenterTelemetryService.Current.Init("1aa04988-ef59-4a4b-9b40-8f838a862018",
                   typeof(Analytics), typeof(Crashes));
            ApplicationInsightsTelemetryService.Current.Init("55f4c3a7-8bd1-4ec0-92b3-717cb9ddde1d");

            SQLitePCL.Batteries.Init();

            UseDefaultConfiguration();
            Forms.SetFlags("StateTriggers_Experimental");
            Forms.Init();

            LoadApplication(new App(new SampleAppiOSInitializer()));

            return base.FinishedLaunching(app, options);
        }
    }

    public class SampleAppiOSInitializer : BitPlatformInitializer
    {

    }
}
