using System.Net.Http;

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
        BlazorWebView.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(App.Current.Properties["Culture"]?.ToString());
        BlazorWebView.Services.GetRequiredService<IPubSubService>().Subscribe(PubSubMessages.CULTURE_CHANGED, async culture =>
        {
            App.Current.Shutdown();
            Application.Restart();
        });
        BlazorWebView.Loaded += async delegate
        {
            await BlazorWebView.WebView.EnsureCoreWebView2Async();

            BlazorWebView.WebView.NavigationCompleted += async delegate
            {
                await BlazorWebView.WebView.ExecuteScriptAsync("Blazor.start()");
            };
        };
    }
}
