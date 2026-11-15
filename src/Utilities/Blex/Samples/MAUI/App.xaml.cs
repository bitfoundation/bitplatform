namespace Bit.Blex.Samples.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new MainPage()) { Title = "Blex Samples" };

    // The OS may stop the app once it is in the background, disposing nothing: write the debounced save now.
    protected override void OnSleep() => Bit.Blex.Maui.BlexPreferencesStorage.NotifySuspending();
}
