using System.Text;

namespace Bit.Belemetry;

/// <summary>Prints exported records to the console - for development only.</summary>
internal sealed class BelemetryConsoleExporter(BelemetryConsoleExporterOptions options) : IBelemetryExporter
{
    public string Name => "console";

    public ValueTask<BelemetryExportResult> ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        var service = batch.Resource.ServiceName;

        if (options.Signals.HasFlag(BelemetrySignals.Traces))
        {
            foreach (var span in batch.Spans)
            {
                text.Append("[belemetry] span ").Append(service).Append(' ').Append(span.Kind).Append(' ').Append(span.Name)
                    .Append(" (").Append(span.Duration.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(" ms)")
                    .Append(" trace=").Append(span.TraceId).Append(" span=").Append(span.SpanId);
                if (span.Status == BelemetryStatusCode.Error) text.Append(" ERROR ").Append(span.StatusMessage);
                AppendAttributes(text, span.Attributes);
                text.AppendLine();
            }
        }

        if (options.Signals.HasFlag(BelemetrySignals.Logs))
        {
            foreach (var log in batch.Logs)
            {
                text.Append("[belemetry] log ").Append(service).Append(' ').Append(log.SeverityText ?? log.Severity.ToString()).Append(' ');
                if (log.EventName is not null) text.Append('<').Append(log.EventName).Append("> ");
                text.Append(log.Body);
                AppendAttributes(text, log.Attributes);
                text.AppendLine();
            }
        }

        if (options.Signals.HasFlag(BelemetrySignals.Metrics))
        {
            foreach (var metric in batch.Metrics)
            {
                foreach (var p in metric.Points)
                {
                    text.Append("[belemetry] metric ").Append(service).Append(' ').Append(metric.Name).Append(' ');
                    text.Append(metric.Type switch
                    {
                        BelemetryMetricType.Histogram => $"count={p.Count} sum={p.Sum} min={p.Min} max={p.Max}",
                        _ => p.IsInteger ? p.LongValue.ToString(System.Globalization.CultureInfo.InvariantCulture) : p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    });
                    if (string.IsNullOrEmpty(metric.Unit) is false) text.Append(' ').Append(metric.Unit);
                    AppendAttributes(text, p.Attributes);
                    text.AppendLine();
                }
            }
        }

        if (text.Length > 0) Console.Write(text.ToString());
        return ValueTask.FromResult(BelemetryExportResult.Success);
    }

    private static void AppendAttributes(StringBuilder text, BelemetryAttributes attributes)
    {
        if (attributes.Count == 0) return;

        text.Append(" {");
        var first = true;
        foreach (var (key, value) in attributes)
        {
            if (first is false) text.Append(", ");
            text.Append(key).Append('=').Append(BelemetryAttributes.ToInvariantString(value));
            first = false;
        }
        text.Append('}');
    }
}
