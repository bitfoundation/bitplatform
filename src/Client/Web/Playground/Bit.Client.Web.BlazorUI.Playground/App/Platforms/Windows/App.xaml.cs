using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml;

namespace Bit.Client.Web.BlazorUI.Playground.Web.WinUI
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            Platform.OnLaunched(args);
        }
    }
}
