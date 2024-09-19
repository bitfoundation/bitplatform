using Microsoft.AspNetCore.Components.WebView.Wpf;

namespace Boilerplate.Client.Windows;

public partial class MainWindow
{
    public MainWindow()
    {
        AppPlatform.IsBlazorHybrid = true;
        var services = new ServiceCollection();
        services.ConfigureServices();
        InitializeComponent();
        AppWebView.Services = services.BuildServiceProvider();
        if (CultureInfoManager.MultilingualEnabled)
        {
            AppWebView.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(
                App.Current.Properties["Culture"]?.ToString() ?? // 1- User settings
                CultureInfo.CurrentUICulture.Name); // 2- OS Settings
        }
        AppWebView.Services.GetRequiredService<IPubSubService>().Subscribe(PubSubMessages.CULTURE_CHANGED, async culture =>
        {
            App.Current.Shutdown();
            Application.Restart();
        });
        AppWebView.Loaded += async delegate
        {
            AppWebView.WebView.DefaultBackgroundColor = ColorTranslator.FromHtml(App.Current.Resources["PrimaryBgColor"].ToString()!);
            await AppWebView.WebView.EnsureCoreWebView2Async();
            var settings = AppWebView.WebView.CoreWebView2.Settings;
            if (AppEnvironment.IsDev() is false)
            {
                settings.IsZoomControlEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
            }
            AppWebView.WebView.NavigationCompleted += async delegate
            {
                await AppWebView.WebView.ExecuteScriptAsync("Blazor.start()");
            };
        };
    }
}
