using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>A browser span. Times are Unix-epoch milliseconds.</summary>
internal sealed class BelemetryRelaySpan
{
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? ParentSpanId { get; set; }
    public string? Name { get; set; }
    public int Kind { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public Dictionary<string, JsonElement>? Attributes { get; set; }
    public BelemetryRelayStatus? Status { get; set; }
    public List<BelemetryRelaySpanEvent>? Events { get; set; }
    public string? Scope { get; set; }
    public int Feature { get; set; }
}
