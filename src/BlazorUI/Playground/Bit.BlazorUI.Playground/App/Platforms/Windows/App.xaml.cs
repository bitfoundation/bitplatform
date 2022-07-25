using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml;

namespace Bit.BlazorUI.Playground.Web.WinUI
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiAppBuilder().Build();
    }
}
