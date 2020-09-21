using Bit.Core.Implementations;
using Bit.ViewModel.Implementations;
using Prism.Ioc;

namespace Bit.CSharpClientSample.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

#if DEBUG
            LocalTelemetryService.Current.Init();
            DebugTelemetryService.Current.Init();
#else
            // ApplicationInsightsTelemetryService.Current.Init("");
#endif

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
