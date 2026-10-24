namespace Bit.Bmage.Samples.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage()) { Title = "Bmage Samples" };
}
