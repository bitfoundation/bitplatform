using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>Browser records relayed to .NET (the "Browser record model" contract in CLAUDE.md).</summary>
internal sealed class BelemetryRelayBatch
{
    public Dictionary<string, JsonElement>? Resource { get; set; }

    public List<BelemetryRelaySpan>? Spans { get; set; }

    public List<BelemetryRelayLog>? Logs { get; set; }
}
