﻿using Microsoft.Extensions.Logging;
using Boilerplate.Client.Maui.Services;

namespace Boilerplate.Client.Maui;

public static partial class MauiProgram
{
    public static void ConfigureServices(this MauiAppBuilder builder)
    {
        // Services being registered here can get injected in Maui (Android, iOS, macOS, Windows)

        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddClientCoreProjectServices();

        services.AddMauiBlazorWebView();

        if (AppEnvironment.IsDev())
        {
            services.AddBlazorWebViewDeveloperTools();
        }

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

        services.AddTransient<MainPage>();
        services.AddTransient<IStorageService, MauiStorageService>();
        services.AddTransient<IBitDeviceCoordinator, MauiDeviceCoordinator>();
        services.AddTransient<IExceptionHandler, MauiExceptionHandler>();
        services.AddTransient<IExternalNavigationService, MauiExternalNavigationService>();

        if (AppPlatform.IsWindows || AppPlatform.IsMacOS)
        {
            services.AddSessioned<ILocalHttpServer, MauiLocalHttpServer>();
        }

#if Android
        services.AddClientMauiProjectAndroidServices();
#elif iOS
        services.AddClientMauiProjectIosServices();
#elif Mac
        services.AddClientMauiProjectMacCatalystServices();
#elif Windows
        services.AddClientMauiProjectWindowsServices();
#endif
    }
}
