using Bit.Belemetry;

namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// The Belemetry configuration both of the demo's hosts share. The client and the server each add
/// their own exporter on top of it - <c>AddBitBelemetryServices</c> runs every configure callback
/// it is given, in order, on the same <see cref="BelemetryOptions"/>.
/// </summary>
public static class DemoTelemetry
{
    /// <summary>The <c>service.name</c> every record of the demo carries.</summary>
    public const string ServiceName = "belemetry-demo";

    /// <summary>The <c>service.namespace</c>.</summary>
    public const string ServiceNamespace = "bitplatform";

    /// <summary>
    /// The URLs that are the demo talking to itself - the OTLP receiver it exports to and the feed
    /// the Live telemetry page polls. Recording them would show every reader a dependency span per
    /// poll, and the export of each one would be recorded in turn.
    /// </summary>
    public static readonly string[] SelfUrls = ["/otlp/", "/api/telemetry"];

    public static void ConfigureShared(BelemetryOptions options)
    {
        options.ServiceName = ServiceName;
        options.ServiceNamespace = ServiceNamespace;

        // Opt-in: nothing leaves the browser until the reader answers the consent banner. This is
        // the library default, written out because the whole consent flow is part of the demo.
        options.Consent.Default = BelemetryFeatures.None;

        // Short intervals so a click shows up on the Live telemetry page within a couple of
        // seconds. The defaults (5 s, 60 s for metrics) are the right ones for a real app.
        options.Browser.FlushInterval = TimeSpan.FromSeconds(2);
        options.Batch.FlushInterval = TimeSpan.FromSeconds(2);
        options.Metrics.ExportInterval = TimeSpan.FromSeconds(10);

        // Off by default; on here so the browser page can demonstrate them.
        options.Browser.LongTasks = true;
        options.Browser.CaptureConsole = true;

        // The demo's own instrumentation (the ".NET instrumentation" page's buttons).
        options.Traces.Sources.Add(DemoDiagnostics.Name);
        options.Metrics.Meters.Add(DemoDiagnostics.Name);

        foreach (var url in SelfUrls) options.Browser.IgnoreUrls.Add(url);

        options.AddProcessor(new DemoNoiseFilter());
    }
}
