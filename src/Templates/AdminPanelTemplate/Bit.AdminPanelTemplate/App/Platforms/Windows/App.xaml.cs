using Microsoft.UI.Xaml;

namespace AdminPanelTemplate.App.Platforms.Windows;

public partial class App
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiAppBuilder().Build();
}
