using Microsoft.UI.Xaml;

namespace TodoTemplate.App.Platforms.Windows;

public partial class App
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiAppBuilder().Build();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        Platform.OnLaunched(args);
    }
}
