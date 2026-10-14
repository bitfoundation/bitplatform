using Bit.Belemetry;
using Bit.Belemetry.Tests.Harness;

namespace Microsoft.Extensions.DependencyInjection;

public static class HarnessServiceCollectionExtensions
{
    /// <summary>The instrumentation key of the fake Application Insights resource the harness exports to.</summary>
    public const string InstrumentationKey = "11111111-2222-3333-4444-555555555555";

    /// <summary>
    /// Registers Belemetry the way every harness host uses it: exporting to the capturing receiver at
    /// <paramref name="collectorOrigin"/> over OTLP/JSON and the Azure Monitor envelope format both, with flush
    /// intervals short enough for a test to wait on. Every host calls this, so the options under test are identical
    /// wherever the harness runs.
    /// </summary>
    /// <param name="collectorOrigin">The origin that serves <c>/test-collector</c>, e.g. <c>http://127.0.0.1:5400</c>.</param>
    /// <param name="collectServerTelemetry">The value of <see cref="BelemetryServerOptions.CollectDotnetTelemetry"/>.</param>
    /// <param name="configure">Extra configuration, e.g. a host-specific exporter.</param>
    public static IServiceCollection AddBelemetryHarness(this IServiceCollection services, string collectorOrigin, bool collectServerTelemetry = false, Action<BelemetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(collectorOrigin);

        var collector = collectorOrigin.TrimEnd('/') + HarnessCollectorFilter.CollectorPath;

        services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "belemetry-harness";
            o.ServiceVersion = "1.0.0";
            o.Environment = "Test";
            // The harness must not pick up an OTEL_* or APPLICATIONINSIGHTS_* variable of the machine running it.
            o.UseEnvironmentVariables = false;

            o.AddOtlpExporter(collector, otlp => otlp.Protocol = BelemetryOtlpProtocol.HttpJson);
            o.AddAzureMonitorExporter($"InstrumentationKey={InstrumentationKey};IngestionEndpoint={collector}/");

            o.Browser.FlushInterval = TimeSpan.FromMilliseconds(500);
            o.Batch.FlushInterval = TimeSpan.FromMilliseconds(500);
            o.Metrics.ExportInterval = TimeSpan.FromSeconds(2);

            o.Traces.Sources.Add(HarnessTelemetry.Name);
            o.Metrics.Meters.Add(HarnessTelemetry.Name);

            // Each test reloads pages and grants and revokes consent on purpose; a request persisted by one test and
            // retried during the next would show up there.
            o.Persistence.Enabled = false;

            o.Server.CollectDotnetTelemetry = collectServerTelemetry;
            // The request logs of every export would otherwise be exported by the next one.
            o.Logs.ExcludedCategories.Add("Microsoft.AspNetCore");
            o.AddProcessor(new HarnessCollectorFilter());

            configure?.Invoke(o);
        });

        return services;
    }
}
