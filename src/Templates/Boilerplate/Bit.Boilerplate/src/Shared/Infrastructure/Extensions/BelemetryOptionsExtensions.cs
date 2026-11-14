//+:cnd:noEmit
using System.Diagnostics.Metrics;

namespace Bit.Belemetry;

public static class BelemetryOptionsExtensions
{
    extension(BelemetryOptions options)
    {
        public BelemetryOptions ConfigureAppTelemetry(IConfiguration configuration)
        {
            options.AddTraceSources(ActivitySource.Current.Name);
            options.AddMeters(Meter.Current.Name);

            options.Browser.TrackPageVisitTime = true;

            if (options.OtlpExporters.Count is 0 && string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]) is false)
            {
                options.AddOtlpExporter(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!, otlp =>
                {
                    otlp.Protocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]?.Trim().ToLowerInvariant() switch
                    {
                        "grpc" => BelemetryOtlpProtocol.Grpc,
                        "http/json" => BelemetryOtlpProtocol.HttpJson,
                        _ => BelemetryOtlpProtocol.HttpProtobuf
                    };

                    foreach (var header in (configuration["OTEL_EXPORTER_OTLP_HEADERS"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var parts = header.Split('=', 2, StringSplitOptions.TrimEntries);
                        if (parts.Length is 2 && parts[0].Length > 0)
                        {
                            otlp.Headers[parts[0]] = Uri.UnescapeDataString(parts[1]);
                        }
                    }
                });
            }

            //#if (appInsights == true)
            if (options.AzureMonitorExporters.Count is 0 && string.IsNullOrWhiteSpace(configuration["ApplicationInsights:ConnectionString"]) is false)
            {
                options.AddAzureMonitorExporter(configuration["ApplicationInsights:ConnectionString"]!);
            }
            //#endif

            //#if (sentry == true)
            if (options.SentryExporters.Count is 0 && string.IsNullOrWhiteSpace(configuration["Logging:Sentry:Dsn"]) is false)
            {
                options.AddSentryExporter(configuration["Logging:Sentry:Dsn"]!);
            }
            //#endif

            return options;
        }

        public BelemetryOptions AddTraceSources(params string[] sources)
        {
            options.Traces.Sources.AddRange(sources.Except(options.Traces.Sources));
            return options;
        }

        public BelemetryOptions AddMeters(params string[] meters)
        {
            options.Metrics.Meters.AddRange(meters.Except(options.Metrics.Meters));
            return options;
        }
    }
}
