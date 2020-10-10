using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Bit.CSharpClientSample.UWP
{
    public partial class App
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

                UseDefaultConfiguration();
                Xamarin.Forms.Forms.SetFlags("StateTriggers_Experimental");
                Xamarin.Forms.Forms.Init(e, new Assembly[]
                {

                }.Union(GetBitRendererAssemblies()));

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }

            Window.Current.Activate();

            base.OnLaunched(e);
        }
    }
}
