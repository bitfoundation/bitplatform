using Bit.View;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Bit.CSharpClient.Controls.Samples
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            BitCSharpClientControls.XamlInit();

            MainPage = new NavigationPage(new MainPage());
        }
    }
}
