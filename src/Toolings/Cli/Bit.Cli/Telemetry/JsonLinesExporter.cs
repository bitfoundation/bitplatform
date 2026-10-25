using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry;

namespace Bit.Cli.Telemetry;

public sealed class JsonLinesExporter(TextWriter writer) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            var item = new Dictionary<string, object?>
            {
                ["name"] = activity.DisplayName,
                ["kind"] = activity.Kind.ToString(),
                ["traceId"] = activity.TraceId.ToHexString(),
                ["spanId"] = activity.SpanId.ToHexString(),
                ["parentSpanId"] = activity.ParentSpanId == default ? null : activity.ParentSpanId.ToHexString(),
                ["durationMs"] = Math.Round(activity.Duration.TotalMilliseconds),
                ["status"] = activity.Status.ToString(),
                ["tags"] = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value?.ToString()),
                ["events"] = activity.Events.Select(e => new { name = e.Name, tags = e.Tags.ToDictionary(t => t.Key, t => t.Value?.ToString()) }).ToArray()
            };

            lock (writer)
            {
                writer.WriteLine("telemetry " + JsonSerializer.Serialize(item));
            }
        }

        return ExportResult.Success;
    }
}
