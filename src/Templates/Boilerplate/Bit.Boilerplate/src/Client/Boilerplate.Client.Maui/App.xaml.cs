[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    private readonly Page mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();

        this.mainPage = new NavigationPage(mainPage);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(mainPage) { };
    }
}
