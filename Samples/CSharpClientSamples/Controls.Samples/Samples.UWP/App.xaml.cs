using Bit.View;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Bit.CSharpClient.Controls.Samples.UWP
{
    sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();

                BitCSharpClientControls.Init();

                Xamarin.Forms.Forms.Init(e, new Assembly[] { }.Union(Rg.Plugins.Popup.Popup.GetExtraAssemblies()));

                Rg.Plugins.Popup.Popup.Init();

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }

            Window.Current.Activate();
        }
    }
}
