using Microsoft.Extensions.Logging;
using AdminPanel.Client.Maui.Services;
using Microsoft.Extensions.Options;

namespace AdminPanel.Client.Maui;

public static partial class MauiProgram
{
    public static void ConfigureServices(this MauiAppBuilder builder)
    {
        // Services being registered here can get injected in Maui (Android, iOS, macOS, Windows)

        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddClientCoreProjectServices(builder.Configuration);

        services.AddMauiBlazorWebView();

        if (AppEnvironment.IsDev())
        {
        }

            services.AddBlazorWebViewDeveloperTools();
        services.AddSessioned(sp =>
        {
            var handler = sp.GetRequiredService<HttpMessageHandler>();
            HttpClient httpClient = new(handler)
            {
                BaseAddress = new Uri(configuration.GetServerAddress(), UriKind.Absolute)
            };
            return httpClient;
        });

        builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

        if (AppEnvironment.IsDev())
        {
            builder.Logging.AddDebug();
        }
            builder.Logging.AddBrowserConsoleLogger();

        builder.Logging.AddConsole();

        if (AppPlatform.IsWindows)
        {
            builder.Logging.AddEventLog();
        }

        builder.Logging.AddEventSourceLogger();

        
        if (Microsoft.AppCenter.AppCenter.Configured)
        {
            builder.Logging.AddAppCenter(options => { });
        }

        var connectionString = configuration.Get<ClientMauiSettings>()!.ApplicationInsights?.ConnectionString;
        if (string.IsNullOrEmpty(connectionString) is false)
        {
            builder.Logging.AddApplicationInsights(config =>
            {
                config.TelemetryInitializers.Add(new MauiTelemetryInitializer());
                config.ConnectionString = connectionString;
            }, options =>
            {
                options.IncludeScopes = true;
            });
        }
        
        services.AddTransient<MainPage>();
        services.AddSingleton(ITelemetryContext.Current = new MauiTelemetryContext());
        services.AddTransient<IStorageService, MauiStorageService>();
        services.AddTransient<IBitDeviceCoordinator, MauiDeviceCoordinator>();
        services.AddTransient<IExceptionHandler, MauiExceptionHandler>();
        services.AddTransient<IExternalNavigationService, MauiExternalNavigationService>();

        if (AppPlatform.IsWindows || AppPlatform.IsMacOS)
        {
            services.AddSessioned<ILocalHttpServer, MauiLocalHttpServer>();
        }

        services.AddOptions<ClientMauiSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => configuration.Get<ClientMauiSettings>()!);

#if Android
        services.AddClientMauiProjectAndroidServices(builder.Configuration);
#elif iOS
        services.AddClientMauiProjectIosServices(builder.Configuration);
#elif Mac
        services.AddClientMauiProjectMacCatalystServices(builder.Configuration);
#elif Windows
        services.AddClientMauiProjectWindowsServices(builder.Configuration);
#endif
    }
}
