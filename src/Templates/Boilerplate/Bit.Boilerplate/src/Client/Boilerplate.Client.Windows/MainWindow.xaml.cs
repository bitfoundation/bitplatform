using Microsoft.AspNetCore.Components.WebView.Wpf;

namespace Boilerplate.Client.Windows;

public partial class MainWindow
{
    public MainWindow()
    {
        AppRenderMode.IsBlazorHybrid = true;
        var services = new ServiceCollection();
        services.ConfigureServices();
        InitializeComponent();
        BlazorWebView.Services = services.BuildServiceProvider();
        if (CultureInfoManager.MultilingualEnabled)
        {
            BlazorWebView.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(App.Current.Properties["Culture"]?.ToString() ?? CultureInfo.CurrentUICulture.Name);
        }
        BlazorWebView.Services.GetRequiredService<IPubSubService>().Subscribe(PubSubMessages.CULTURE_CHANGED, async culture =>
        {
            App.Current.Shutdown();
            Application.Restart();
        });
        BlazorWebView.Loaded += async delegate
        {
            await BlazorWebView.WebView.EnsureCoreWebView2Async();
            var settings = BlazorWebView.WebView.CoreWebView2.Settings;
            if (BuildConfiguration.IsRelease())
            {
                settings.IsZoomControlEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
            }
            BlazorWebView.WebView.NavigationCompleted += async delegate
            {
                await BlazorWebView.WebView.ExecuteScriptAsync("Blazor.start()");
            };
        };
    }
}
