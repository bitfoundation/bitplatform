using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>A browser log record or event. Times are Unix-epoch milliseconds.</summary>
internal sealed class BelemetryRelayLog
{
    public double Time { get; set; }
    public int SeverityNumber { get; set; }
    public string? SeverityText { get; set; }
    public string? Body { get; set; }
    public string? EventName { get; set; }
    public Dictionary<string, JsonElement>? Attributes { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? Scope { get; set; }
    public int Feature { get; set; }
}
