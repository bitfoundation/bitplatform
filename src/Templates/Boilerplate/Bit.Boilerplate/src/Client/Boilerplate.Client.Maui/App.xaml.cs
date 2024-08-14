[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Boilerplate.Client.Maui;

public partial class App
{
    MainPage mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();

        this.mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(mainPage));
	}
}
