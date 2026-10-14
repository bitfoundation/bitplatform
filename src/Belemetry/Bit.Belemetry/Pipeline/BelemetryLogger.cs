using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary>One logger category of <see cref="BelemetryLoggerProvider"/>.</summary>
internal sealed class BelemetryLogger : ILogger
{
    private const string OriginalFormat = "{OriginalFormat}";

    private readonly BelemetryLoggerProvider _provider;
    private readonly BelemetryPipeline _pipeline;
    private readonly BelemetryScope _scope;
    private readonly bool _excluded;
    private readonly LogLevel _minimumLevel;
    private readonly BelemetryLoggingOptions _options;

    public BelemetryLogger(string category, BelemetryLoggerProvider provider, BelemetryPipeline pipeline)
    {
        _provider = provider;
        _pipeline = pipeline;
        _scope = new BelemetryScope(category);
        _options = pipeline.Settings.Options.Logs;
        _minimumLevel = _options.MinimumLevel;

        // Belemetry's own diagnostics would otherwise be exported by the pipeline they describe, failure logs about an
        // export included - an export storm in the making.
        _excluded = category.StartsWith("Bit.Belemetry", StringComparison.Ordinal)
                    || _options.ExcludedCategories.Any(prefix => category.StartsWith(prefix, StringComparison.Ordinal));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _provider.ScopeProvider?.Push(state);

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None
               && logLevel >= _minimumLevel
               && _excluded is false
               && _pipeline.IsCollecting
               && _pipeline.CanRecord(logLevel >= LogLevel.Error ? BelemetryFeatures.Errors : BelemetryFeatures.Logs, allowPending: true);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false || BelemetrySuppression.IsActive) return;

        try
        {
            var now = BelemetryTime.NowUnixNano();
            var record = new BelemetryLogRecord
            {
                TimeUnixNano = now,
                ObservedTimeUnixNano = now,
                Severity = ToSeverity(logLevel),
                SeverityText = logLevel.ToString(),
                Scope = _scope,
                Feature = logLevel >= LogLevel.Error || exception is not null ? BelemetryFeatures.Errors : BelemetryFeatures.Logs,
            };

            record.Body = formatter is null ? state?.ToString() : formatter(state, exception);

            if (state is IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                foreach (var (key, value) in values)
                {
                    if (key == OriginalFormat)
                    {
                        if (_options.IncludeMessageTemplate) record.Attributes.Set(BelemetrySemanticConventions.LogMessageTemplate, value);
                        continue;
                    }
                    record.Attributes.Set(key, value);
                }
            }

            if (eventId.Id != 0) record.Attributes.Set(BelemetrySemanticConventions.LogEventId, eventId.Id);
            if (string.IsNullOrEmpty(eventId.Name) is false) record.Attributes.Set(BelemetrySemanticConventions.LogEventName, eventId.Name);

            if (exception is not null)
            {
                record.Attributes.Set(BelemetrySemanticConventions.ExceptionType, exception.GetType().FullName);
                record.Attributes.Set(BelemetrySemanticConventions.ExceptionMessage, exception.Message);
                record.Attributes.Set(BelemetrySemanticConventions.ExceptionStacktrace, exception.ToString());
            }

            if (_options.IncludeScopes && _provider.ScopeProvider is { } scopes)
            {
                scopes.ForEachScope(static (scope, target) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
                    {
                        foreach (var (key, value) in pairs)
                        {
                            if (key != OriginalFormat) target.Attributes.SetIfAbsent(key, value);
                        }
                    }
                }, record);
            }

            if (Activity.Current is { } activity && activity.IdFormat == ActivityIdFormat.W3C)
            {
                record.TraceId = activity.TraceId.ToHexString();
                record.SpanId = activity.SpanId.ToHexString();
                record.Flags = (int)activity.ActivityTraceFlags;
            }

            _pipeline.Enqueue(record);
        }
        catch
        {
            // Never let telemetry break the code that logged.
        }
    }

    internal static BelemetrySeverity ToSeverity(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => BelemetrySeverity.Trace,
            LogLevel.Debug => BelemetrySeverity.Debug,
            LogLevel.Information => BelemetrySeverity.Info,
            LogLevel.Warning => BelemetrySeverity.Warn,
            LogLevel.Error => BelemetrySeverity.Error,
            LogLevel.Critical => BelemetrySeverity.Fatal,
            _ => BelemetrySeverity.Unspecified,
        };
    }
}
