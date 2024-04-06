using Boilerplate.Client.Windows.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Components.Authorization;
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
        BlazorWebView.Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(App.Current.Properties["Culture"]?.ToString());
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

        _ = SetLoggerAuthenticationState();
    }

    private async Task SetLoggerAuthenticationState()
    {
        async Task SetLoggerAuthenticationStateImpl(AuthenticationState state)
        {
            try
            {
                var user = state.User;
                if (user.IsAuthenticated())
                {
                    // Set firebase, app center and other logger's user id
                    WindowsTelemetryInitializer.AuthenticatedUserId = user.GetUserId().ToString();
                }
                else
                {
                    // Clear firebase, app center and other logger's user id
                    WindowsTelemetryInitializer.AuthenticatedUserId = null;
                }
            }
            catch (Exception exp)
            {
                BlazorWebView.Services.GetRequiredService<IExceptionHandler>().Handle(exp);
            }
        }

        var authManager = BlazorWebView.Services.GetRequiredService<AuthenticationManager>();

        await SetLoggerAuthenticationStateImpl(await authManager.GetAuthenticationStateAsync());

        authManager.AuthenticationStateChanged += async state => await SetLoggerAuthenticationStateImpl(await state);
    }
}
