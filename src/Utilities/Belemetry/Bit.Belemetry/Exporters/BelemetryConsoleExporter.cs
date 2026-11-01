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
                text.Append("[belemetry] span ");
                AppendEscaped(text, service).Append(' ').Append(span.Kind).Append(' ');
                AppendEscaped(text, span.Name)
                    .Append(" (").Append(span.Duration.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(" ms)")
                    .Append(" trace=").Append(span.TraceId).Append(" span=").Append(span.SpanId);
                if (span.Status == BelemetryStatusCode.Error) AppendEscaped(text.Append(" ERROR "), span.StatusMessage);
                AppendAttributes(text, span.Attributes);
                text.AppendLine();
            }
        }

        if (options.Signals.HasFlag(BelemetrySignals.Logs))
        {
            foreach (var log in batch.Logs)
            {
                text.Append("[belemetry] log ");
                AppendEscaped(text, service).Append(' ');
                AppendEscaped(text, log.SeverityText ?? log.Severity.ToString()).Append(' ');
                if (log.EventName is not null) AppendEscaped(text.Append('<'), log.EventName).Append("> ");
                AppendEscaped(text, log.Body);
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
                    text.Append("[belemetry] metric ");
                    AppendEscaped(text, service).Append(' ');
                    AppendEscaped(text, metric.Name).Append(' ');
                    text.Append(metric.Type switch
                    {
                        BelemetryMetricType.Histogram => $"count={p.Count} sum={p.Sum} min={p.Min} max={p.Max}",
                        _ => p.IsInteger ? p.LongValue.ToString(System.Globalization.CultureInfo.InvariantCulture) : p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    });
                    if (string.IsNullOrEmpty(metric.Unit) is false) AppendEscaped(text.Append(' '), metric.Unit);
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
            AppendEscaped(text, key).Append('=');
            AppendEscaped(text, BelemetryAttributes.ToInvariantString(value));
            first = false;
        }
        text.Append('}');
    }

    private static StringBuilder AppendEscaped(StringBuilder text, string? value)
    {
        if (value is null) return text;

        foreach (var c in value)
        {
            if (c == '\n') text.Append("\\n");
            else if (c == '\r') text.Append("\\r");
            else if ((c != '\t' && char.IsControl(c)) || c is '\u2028' or '\u2029') text.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
            else text.Append(c);
        }

        return text;
    }
}
