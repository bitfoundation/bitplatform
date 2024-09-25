//+:cnd:noEmit
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Boilerplate.Client.Windows.Services;
using Boilerplate.Client.Windows.Configuration;

namespace Boilerplate.Client.Windows;

public static partial class Program
{
    public static void AddClientWindowsProjectServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in windows project only.

        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddClientConfigurations();
        var configuration = configurationBuilder.Build();
        services.TryAddTransient<IConfiguration>(sp => configuration);

        services.TryAddSingleton(sp =>
        {
            var handler = sp.GetRequiredService<HttpMessageHandler>();
            HttpClient httpClient = new(handler)
            {
                BaseAddress = new Uri(configuration.GetServerAddress(), UriKind.Absolute)
            };
            return httpClient;
        });

        services.AddWpfBlazorWebView();
        if (AppEnvironment.IsDev())
        {
            services.AddBlazorWebViewDeveloperTools();
        }

        services.TryAddTransient<IStorageService, WindowsStorageService>();
        services.TryAddTransient<IBitDeviceCoordinator, WindowsDeviceCoordinator>();
        services.TryAddTransient<IExceptionHandler, WindowsExceptionHandler>();
        services.AddSingleton<ILocalHttpServer, WindowsLocalHttpServer>();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddEventLog();
            loggingBuilder.AddEventSourceLogger();
            if (AppEnvironment.IsDev())
            {
                loggingBuilder.AddDebug();
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
        });

        services.AddOptions<WindowsUpdateSettings>()
            .Bind(configuration.GetRequiredSection(nameof(WindowsUpdateSettings)))
            .ValidateOnStart();

        services.AddClientCoreProjectServices();
    }
}
