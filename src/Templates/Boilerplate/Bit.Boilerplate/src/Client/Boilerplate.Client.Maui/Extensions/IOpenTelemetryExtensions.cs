//+:cnd:noEmit
//#if (appInsights == true)
using Azure.Monitor.OpenTelemetry.Exporter;
//#endif
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class IOpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetry<TProcessor>(this IServiceCollection services, IConfiguration configuration, string serviceName)
        where TProcessor : BaseProcessor<Activity>
    {
        var useOtlpExporter = string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false;

        bool useOpenTelemetry = useOtlpExporter == true;

        //#if (appInsights == true)
        var appInsightsConnectionString = string.IsNullOrWhiteSpace(configuration["ApplicationInsights:ConnectionString"]) is false ? configuration["ApplicationInsights:ConnectionString"] : null;
        useOpenTelemetry = useOpenTelemetry || string.IsNullOrEmpty(appInsightsConnectionString) is false;
        //#endif

        if (useOpenTelemetry is false)
            return services;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddProcessor<TProcessor>()
                .AddSource(ActivitySource.Current.Name)
                .AddSource(serviceName))
            .WithMetrics(metrics => metrics
                .AddMeter(Meter.Current.Name)
                .AddRuntimeInstrumentation());

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddOpenTelemetry(logging =>
            {
                var resBuilder = logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                if (useOtlpExporter)
                {
                    resBuilder.AddOtlpExporter();
                }
                //#if (appInsights == true)
                if (string.IsNullOrEmpty(appInsightsConnectionString) is false)
                {
                    resBuilder.AddAzureMonitorLogExporter(c => c.ConnectionString = appInsightsConnectionString);
                }
                //#endif
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
        });

        return services;
    }
}
