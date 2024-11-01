//+:cnd:noEmit
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Boilerplate.Client.Windows.Services;

namespace Boilerplate.Client.Windows;

public static partial class Program
{
    public static void AddClientWindowsProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in windows project only.
        services.AddClientCoreProjectServices(configuration);

        services.AddScoped<IExceptionHandler, WindowsExceptionHandler>();
        services.AddScoped<IBitDeviceCoordinator, WindowsDeviceCoordinator>();
        services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<HttpMessageHandler>();
            HttpClient httpClient = new(handler)
            {
                BaseAddress = new Uri(configuration.GetServerAddress(), UriKind.Absolute)
            };
            return httpClient;
        });

        services.AddSingleton(sp => configuration);
        services.AddSingleton<IStorageService, WindowsStorageService>();
        services.AddSingleton<ILocalHttpServer, WindowsLocalHttpServer>();
        services.AddSingleton(sp => configuration.Get<ClientWindowsSettings>()!);
        services.AddSingleton(ITelemetryContext.Current = new WindowsTelemetryContext());
        //#if (notification == true)
        services.AddSingleton<IPushNotificationService, WindowsPushNotificationService>();
        //#endif

        services.AddWpfBlazorWebView();
        if (AppEnvironment.IsDev())
        {
            services.AddBlazorWebViewDeveloperTools();
        }

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddEventLog();
            loggingBuilder.AddEventSourceLogger();
            if (AppEnvironment.IsDev())
            {
                loggingBuilder.AddDebug();
                loggingBuilder.AddBrowserConsoleLogger();
            }
            loggingBuilder.AddConsole();
            //#if (appCenter == true)
            if (Microsoft.AppCenter.AppCenter.Configured)
            {
                loggingBuilder.AddAppCenter(options => { });
            }
            //#endif
            //#if (appInsights == true)
            loggingBuilder.AddApplicationInsights(config =>
            {
                config.TelemetryInitializers.Add(new WindowsTelemetryInitializer());
                var connectionString = configuration.Get<ClientWindowsSettings>()!.ApplicationInsights?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) is false)
                {
                    config.ConnectionString = connectionString;
                }
            }, options =>
            {
                options.IncludeScopes = true;
            });
            //#endif
        });

        services.AddOptions<ClientWindowsSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
