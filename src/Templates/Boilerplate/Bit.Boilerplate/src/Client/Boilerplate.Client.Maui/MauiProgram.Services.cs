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

        services.AddMauiBlazorWebView();

        if (BuildConfiguration.IsDebug())
        {
            services.AddBlazorWebViewDeveloperTools();
        }

        Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);

        services.TryAddTransient(sp =>
        {
            var handler = sp.GetRequiredKeyedService<DelegatingHandler>("DefaultMessageHandler");
            HttpClient httpClient = new(handler)
            {
                BaseAddress = apiServerAddress
            };
            return httpClient;
        });

        builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

        if (BuildConfiguration.IsDebug())
        {
            builder.Logging.AddDebug();
            builder.Logging.AddConsole();
        }

        if (OperatingSystem.IsWindows())
        {
            builder.Logging.AddEventLog();
        }

        builder.Logging.AddEventSourceLogger();

        //+:cnd:noEmit
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

#if ANDROID
        services.AddClientMauiProjectAndroidServices();
#elif iOS
        services.AddClientMauiProjectIosServices();
#elif Mac
        services.AddClientMauiProjectMacCatalystServices();
#elif Windows
        services.AddClientMauiProjectWindowsServices();
#endif

        services.AddClientCoreProjectServices();
    }
}
