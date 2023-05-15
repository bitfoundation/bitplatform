[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace TodoTemplate.Client.App;

public partial class App
{
    public App(MainPage mainPage)
    {
        InitializeComponent();

        MainPage = new NavigationPage(mainPage);
    }
}
