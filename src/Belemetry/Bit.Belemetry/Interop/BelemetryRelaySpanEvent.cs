using System.Text.Json;

namespace Bit.Belemetry;

internal sealed class BelemetryRelaySpanEvent
{
    public string? Name { get; set; }
    public double Time { get; set; }
    public Dictionary<string, JsonElement>? Attributes { get; set; }
}
