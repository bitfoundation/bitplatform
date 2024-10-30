using System.Net.Http;
using Microsoft.Extensions.Logging;
using AdminPanel.Client.Windows.Services;
using Microsoft.Extensions.Options;

namespace AdminPanel.Client.Windows;

public static partial class Program
{
    public static void AddClientWindowsProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in windows project only.

        services.AddTransient(sp => configuration);

        services.AddClientCoreProjectServices(configuration);

        services.AddSessioned(sp =>
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

        services.AddSingleton(ITelemetryContext.Current = new WindowsTelemetryContext());
        services.AddTransient<IStorageService, WindowsStorageService>();
        services.AddTransient<IBitDeviceCoordinator, WindowsDeviceCoordinator>();
        services.AddTransient<IExceptionHandler, WindowsExceptionHandler>();
        services.AddSessioned<ILocalHttpServer, WindowsLocalHttpServer>();
        services.AddScoped<IPushNotificationService, WindowsPushNotificationService>();

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
            if (Microsoft.AppCenter.AppCenter.Configured)
            {
                loggingBuilder.AddAppCenter(options => { });
            }
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
        });

        services.AddOptions<ClientWindowsSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => configuration.Get<ClientWindowsSettings>()!);
    }
}
