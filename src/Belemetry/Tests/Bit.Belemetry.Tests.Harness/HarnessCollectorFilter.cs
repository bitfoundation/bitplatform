namespace Bit.Belemetry.Tests.Harness;

/// <summary>
/// Drops the server spans of requests to the capturing receiver. With the server pipeline on, every export is itself a
/// request the ASP.NET Core instrumentation traces, whose span the next export carries - an endless trickle of
/// telemetry about telemetry that would only bury what the tests look for.
/// </summary>
public sealed class HarnessCollectorFilter : IBelemetryProcessor
{
    public const string CollectorPath = "/test-collector";

    public bool OnSpan(BelemetrySpan span)
    {
        var path = span.Attributes.GetString("url.path") ?? span.Attributes.GetString("http.route") ?? span.Attributes.GetString("http.target");
        return path is null || path.StartsWith(CollectorPath, StringComparison.OrdinalIgnoreCase) is false;
    }
}
