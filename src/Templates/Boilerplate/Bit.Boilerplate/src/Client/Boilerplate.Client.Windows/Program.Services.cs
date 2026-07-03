//+:cnd:noEmit
using OpenTelemetry;
using OpenTelemetry.Resources;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
//#if (appInsights == true)
using Azure.Monitor.OpenTelemetry.Exporter;
//#endif
using Boilerplate.Client.Windows.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

namespace Boilerplate.Client.Windows;

public static partial class Program
{
    public static void AddClientWindowsProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in windows project only.
        services.AddClientCoreProjectServices(configuration);

        services.AddScoped<IWebAuthnService, WindowsWebAuthnService>();
        services.AddScoped<IExceptionHandler, WindowsExceptionHandler>();
        services.AddScoped(sp => (ClientExceptionHandlerBase)sp.GetRequiredService<IExceptionHandler>());

        services.AddScoped<IAppUpdateService, WindowsAppUpdateService>();
        services.AddScoped<IBitDeviceCoordinator, WindowsDeviceCoordinator>();

        services.AddScoped<HttpClient>(sp =>
        {
            var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
            var httpClient = new HttpClient(handlerFactory.Invoke())
            {
                BaseAddress = new Uri(configuration.GetServerAddress(), UriKind.Absolute)
            };
            if (sp.GetRequiredService<ClientWindowsSettings>().WebAppUrl is Uri origin)
            {
                httpClient.DefaultRequestHeaders.Add("X-Origin", origin.ToString());
            }
            return httpClient;
        });

        services.AddSingleton(sp => configuration);
        services.AddSingleton<IStorageService, WindowsStorageService>();
        services.AddSingleton<ILocalHttpServer, WindowsLocalHttpServer>();

        ClientWindowsSettings settings = new();
        configuration.Bind(settings);
        services.AddSingleton(sp =>
        {
            return settings;
        });
        services.AddSingleton(ITelemetryContext.Current!);
        //#if (notification == true)
        services.AddSingleton<IPushNotificationService, WindowsPushNotificationService>();
        //#endif

        services.AddWindowsFormsBlazorWebView();
        services.AddBlazorWebViewDeveloperTools();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ConfigureLoggers(configuration);
            loggingBuilder.AddEventSourceLogger();

            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
                configuration.Bind("Logging:OpenTelemetry", options);
            });

            loggingBuilder.AddEventLog(options => configuration.Bind("Logging:EventLog", options));
        });

        var openTelemetry = services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(Meter.Current.Name);
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(ActivitySource.Current.Name);
            })
            .ConfigureResource(resource =>
            {
                resource.AddAttributes([new("service.name", Application.ProductName!)]);
            });

        var useOtlpExporter = string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false
            || string.IsNullOrEmpty(configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]) is false;

        if (useOtlpExporter)
        {
            openTelemetry.UseOtlpExporter();
        }

        //#if (appInsights == true)
        if (string.IsNullOrEmpty(settings.ApplicationInsights?.ConnectionString) is false)
        {
            openTelemetry.UseAzureMonitorExporter(options =>
            {
                configuration.Bind("ApplicationInsights", options);
            });
        }
        //#endif

        services.AddOptions<ClientWindowsSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
