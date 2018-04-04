using Bit.ViewModel.Implementations;
using Prism.Ioc;

namespace Bit.CSharpClientSample.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

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
