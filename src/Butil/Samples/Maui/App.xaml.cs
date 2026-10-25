namespace Bit.Butil.Samples.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // Spelled out because this namespace is nested in Bit.Butil, whose Window service would otherwise shadow MAUI's.
    protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState) => new(new MainPage());
}
