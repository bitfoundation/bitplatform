using Microsoft.Extensions.Logging;
using Boilerplate.Client.Maui.Services;

namespace Boilerplate.Client.Maui;

public static partial class MauiProgram
{
    public static void ConfigureServices(this MauiAppBuilder builder)
    {
        // Services being registered here can get injected in Maui (Android, iOS, macOS, Windows)
        var services = builder.Services;
        var configuration = builder.Configuration;
        services.AddClientCoreProjectServices(builder.Configuration);

        services.AddTransient<MainPage>();

        services.AddScoped<IExceptionHandler, MauiExceptionHandler>();
        services.AddScoped<IBitDeviceCoordinator, MauiDeviceCoordinator>();
        services.AddScoped<IExternalNavigationService, MauiExternalNavigationService>();
        services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<HttpMessageHandler>();
            HttpClient httpClient = new(handler)
            {
                BaseAddress = new Uri(configuration.GetServerAddress(), UriKind.Absolute)
            };
            return httpClient;
        });

        services.AddSingleton<IStorageService, MauiStorageService>();
        var settings = new ClientMauiSettings();
        configuration.Bind(settings);
        services.AddSingleton(sp =>
        {
            return settings;
        });
        services.AddSingleton(ITelemetryContext.Current!);
        if (AppPlatform.IsWindows || AppPlatform.IsMacOS)
        {
            services.AddSingleton<ILocalHttpServer, MauiLocalHttpServer>();
        }

        services.AddMauiBlazorWebView();
        services.AddBlazorWebViewDeveloperTools();

        builder.Logging.ConfigureLoggers(configuration);
        builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

        builder.Logging.AddEventSourceLogger();

        if (AppPlatform.IsWindows)
        {
            builder.Logging.AddEventLog(options => configuration.GetRequiredSection("Logging:EventLog").Bind(options));
        }

        //+:cnd:noEmit
        //#if (appInsights == true)
        if (string.IsNullOrEmpty(settings.ApplicationInsights?.ConnectionString) is false)
        {
            builder.Logging.AddApplicationInsights(config =>
            {
                config.TelemetryInitializers.Add(new MauiAppInsightsTelemetryInitializer());
                configuration.GetRequiredSection("ApplicationInsights").Bind(config);
            }, options => configuration.GetRequiredSection("Logging:ApplicationInsights").Bind(options));
        }
        //#endif
        //-:cnd:noEmit

        services.AddOptions<ClientMauiSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
