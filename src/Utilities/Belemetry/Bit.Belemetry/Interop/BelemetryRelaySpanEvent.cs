using System.Text.Json;

namespace Bit.Belemetry;

internal sealed class BelemetryRelaySpanEvent
{
    public string? Name { get; set; }
    public double Time { get; set; }
    public Dictionary<string, JsonElement>? Attributes { get; set; }

    /// <summary>For an exception event: the page's error limits dropped it as a record of its own (<see cref="BelemetrySpanEvent.Limited"/>).</summary>
    public bool? Limited { get; set; }
}
