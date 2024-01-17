using System.Net.Http;

namespace Boilerplate.Client.Windows;

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
