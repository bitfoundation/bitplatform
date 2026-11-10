using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>Browser records relayed to .NET (the "Browser record model" contract in CLAUDE.md).</summary>
internal sealed class BelemetryRelayBatch
{
    public Dictionary<string, JsonElement>? Resource { get; set; }

    public List<BelemetryRelaySpan>? Spans { get; set; }

    public List<BelemetryRelayLog>? Logs { get; set; }

    /// <summary>
    /// The ratio the page sampled its sessions at (<c>sampling.sessionRatio</c>), which may not be this process's
    /// <see cref="BelemetryBrowserOptions.SessionSampleRatio"/>: a page another side of an Auto app started first keeps that
    /// side's configuration. Null from a page that does not send it.
    /// </summary>
    public double? SessionRatio { get; set; }
}
