namespace Bit.Butil.Demo.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
	{
		return new Microsoft.Maui.Controls.Window(new NavigationPage(new MainPage()));
	}
}
