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

            // The clients have no endpoint but this one (See GetBelemetryOtlpEndpoint).
            var browserEndpoint = configuration.GetBelemetryOtlpEndpoint();
            var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

            if (options.OtlpExporters.Count is 0 && (string.IsNullOrWhiteSpace(endpoint) is false || browserEndpoint is not null))
            {
                options.AddOtlpExporter(string.IsNullOrWhiteSpace(endpoint) ? browserEndpoint!.ToString() : endpoint, otlp =>
                {
                    otlp.Protocol = string.IsNullOrWhiteSpace(endpoint) ? BelemetryOtlpProtocol.HttpProtobuf : configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]?.Trim().ToLowerInvariant() switch
                    {
                        "grpc" => BelemetryOtlpProtocol.Grpc,
                        "http/json" => BelemetryOtlpProtocol.HttpJson,
                        _ => BelemetryOtlpProtocol.HttpProtobuf
                    };
                    otlp.BrowserEndpoint = browserEndpoint;

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
            if (options.SentryExporters.Count is 0 && string.IsNullOrWhiteSpace(configuration["Sentry:Dsn"]) is false)
            {
                options.AddSentryExporter(configuration["Sentry:Dsn"]!);
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

    extension(IConfiguration configuration)
    {
        /// <summary>
        /// Belemetry:OtlpEndpoint, where the page and the Windows, Android and iOS apps post over OTLP/HTTP when the
        /// endpoint the servers send to is out of their reach (See Shared/appsettings.json). Only an absolute HTTP(S)
        /// address without user info, which the page would show to every visitor; anything else is ignored. The CSP
        /// reads it here too, so it allows the address the page posts to.
        /// </summary>
        public Uri? GetBelemetryOtlpEndpoint()
            => Uri.TryCreate(configuration["Belemetry:OtlpEndpoint"], UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && uri.UserInfo is "" ? uri : null;
    }
}
