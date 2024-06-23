using System.Net.Http;

namespace Bit.BlazorUI.Demo.Client.Windows;

public partial class MainWindow
{
    public MainWindow()
    {
        AppRenderMode.IsBlazorHybrid = true;
        var services = new ServiceCollection();
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddClientConfigurations();
        var configuration = configurationBuilder.Build();
        services.AddTransient<IConfiguration>(sp => configuration);
        Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);
        services.AddTransient(sp =>
        {
            HttpClient httpClient = new()
            {
                BaseAddress = apiServerAddress
            };
            return httpClient;
        });
        services.AddWpfBlazorWebView();
        if (BuildConfiguration.IsDebug())
        {
            services.AddBlazorWebViewDeveloperTools();
        }
        services.AddWindowsServices();
        InitializeComponent();
        BlazorWebView.Services = services.BuildServiceProvider();
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
