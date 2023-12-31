namespace Bit.Butil.Demo.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new MainPage());
    }
}
