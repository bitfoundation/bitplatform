using Boilerplate.Client.Maui.Services;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Maui;

public static partial class MauiProgram
{
    public static void ConfigureServices(this MauiAppBuilder builder)
    {
        // Services being registered here can get injected in Maui (Android, iOS, macOS, Windows)

        var services = builder.Services;
        var configuration = builder.Configuration;

#if Android
        services.AddClientMauiProjectAndroidServices();
#elif iOS
        services.AddClientMauiProjectIosServices();
#elif Mac
        services.AddClientMauiProjectMacCatalystServices();
#elif Windows
        services.AddClientMauiProjectWindowsServices();
#endif

        services.AddMauiBlazorWebView();

        if (AppEnvironment.IsDev())
        {
            services.AddBlazorWebViewDeveloperTools();
        }

        Uri.TryCreate(configuration.GetServerAddress(), UriKind.Absolute, out var serverAddress);

        services.TryAddSingleton(sp =>
        {
            var handler = sp.GetRequiredKeyedService<DelegatingHandler>("DefaultMessageHandler");
            HttpClient httpClient = new(handler)
            {
                BaseAddress = serverAddress
            };
            return httpClient;
        });

        builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

        if (AppEnvironment.IsDev())
        {
            builder.Logging.AddDebug();
        }

        builder.Logging.AddConsole();

        if (AppPlatform.IsWindows)
        {
            builder.Logging.AddEventLog();
        }

        builder.Logging.AddEventSourceLogger();

        //+:cnd:noEmit

        //#if (appCenter == true)
        if (Microsoft.AppCenter.AppCenter.Configured)
        {
            builder.Logging.AddAppCenter(options => { });
        }
        //#endif

        //#if (appInsights == true)
        builder.Logging.AddApplicationInsights(config =>
        {
            config.TelemetryInitializers.Add(new MauiTelemetryInitializer());
            var connectionString = configuration["ApplicationInsights:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString) is false)
            {
                config.ConnectionString = connectionString;
            }
        }, options =>
        {
            options.IncludeScopes = true;
        });
        //#endif
        //-:cnd:noEmit

        services.TryAddTransient<MainPage>();
        services.TryAddTransient<IStorageService, MauiStorageService>();
        services.TryAddSingleton<IBitDeviceCoordinator, MauiDeviceCoordinator>();
        services.TryAddTransient<IExceptionHandler, MauiExceptionHandler>();
        services.TryAddTransient<IExternalNavigationService, MauiExternalNavigationService>();

        if (AppPlatform.IsWindows || AppPlatform.IsMacOS)
        {
            services.AddSingleton<ILocalHttpServer, MauiLocalHttpServer>();
        }

        services.AddClientCoreProjectServices();
    }
}
