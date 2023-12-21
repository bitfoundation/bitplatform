using System.Net.Http;

namespace Boilerplate.Client.WinExe;

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
            var handler = sp.GetRequiredKeyedService<HttpMessageHandler>("DefaultMessageHandler");
            HttpClient httpClient = new(handler)
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
            while ((await BlazorWebView.WebView.ExecuteScriptAsync("Blazor.start()")) is "null")
                await Task.Yield();
        };
    }
}
