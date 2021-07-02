using Bit.Core.Implementations;
using Bit.ViewModel.Implementations;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Prism.Ioc;

namespace Bit.CSharpClientSample.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

            LocalTelemetryService.Current.Init();
            DebugTelemetryService.Current.Init();
            ApplicationInsightsTelemetryService.Current.Init("55f4c3a7-8bd1-4ec0-92b3-717cb9ddde1d");
            AppCenterTelemetryService.Current.Init("11864b27-c19a-425f-b8b1-4d80f20c9a1d",
                   typeof(Analytics), typeof(Crashes));

            LoadApplication(new CSharpClientSample.App(new UwpInitializer()));
        }
    }

    public class UwpInitializer : BitPlatformInitializer
    {
        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
        }
    }
}
