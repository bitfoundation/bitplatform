using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>The scoped <see cref="IBelemetry"/>.</summary>
internal sealed class BelemetryService(BelemetrySettings settings, BelemetryPipeline pipeline, BelemetryConsentState consent, BelemetryJsBridge bridge) : IBelemetry
{
    private static readonly BelemetryScope _scope = new("Bit.Belemetry.Api", BelemetrySettings.SdkVersion);

    public void TrackEvent(string name, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (UseBrowser())
        {
            _ = bridge.TrackEventAsync(name, attributes);
            return;
        }

        var record = CreateRecord(BelemetryFeatures.Events, BelemetrySeverity.Info, name, attributes);
        record.EventName = name;
        pipeline.Enqueue(record);
    }

    public void TrackPageView(string? name = null, string? url = null, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        if (UseBrowser())
        {
            _ = bridge.TrackPageViewAsync(name, url, attributes);
            return;
        }

        var record = CreateRecord(BelemetryFeatures.PageViews, BelemetrySeverity.Info, null, attributes);
        record.EventName = BelemetrySemanticConventions.PageViewEvent;
        record.Attributes.Set(BelemetrySemanticConventions.PageTitle, name);
        record.Attributes.Set("browser.navigation.type", "manual");
        if (url is not null)
        {
            record.Attributes.Set(BelemetrySemanticConventions.UrlFull, url);
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) record.Attributes.Set(BelemetrySemanticConventions.UrlPath, uri.AbsolutePath);
        }
        pipeline.Enqueue(record);
    }

    public void TrackException(Exception exception, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (UseBrowser())
        {
            _ = bridge.TrackExceptionAsync(exception.GetType().FullName ?? "Exception", exception.Message, exception.ToString(), attributes);
            return;
        }

        var record = CreateRecord(BelemetryFeatures.Errors, BelemetrySeverity.Error, exception.Message, attributes);
        record.EventName = BelemetrySemanticConventions.ExceptionEvent;
        record.SeverityText = "Error";
        record.Attributes.Set(BelemetrySemanticConventions.ExceptionType, exception.GetType().FullName);
        record.Attributes.Set(BelemetrySemanticConventions.ExceptionMessage, exception.Message);
        record.Attributes.Set(BelemetrySemanticConventions.ExceptionStacktrace, exception.ToString());
        pipeline.Enqueue(record);
    }

    public async ValueTask SetUserAsync(string? userId)
    {
        if (settings.IsServer is false) consent.SetUser(userId);
        await bridge.SetUserAsync(userId);
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        if (bridge.IsStarted) await bridge.FlushAsync();
        await pipeline.FlushAsync(includeMetrics: true, cancellationToken);
    }

    /// <summary>
    /// On the server, user-attributed calls go to that user's browser so their consent applies - unless no browser is
    /// connected and the server pipeline can take them.
    /// </summary>
    private bool UseBrowser() => settings.IsServer && (bridge.IsStarted || settings.CollectDotnet is false);

    private static BelemetryLogRecord CreateRecord(BelemetryFeatures feature, BelemetrySeverity severity, string? body, IReadOnlyDictionary<string, object?>? attributes)
    {
        var now = BelemetryTime.NowUnixNano();
        var record = new BelemetryLogRecord
        {
            TimeUnixNano = now,
            ObservedTimeUnixNano = now,
            Severity = severity,
            SeverityText = severity.ToString().ToUpperInvariant(),
            Body = body,
            Scope = _scope,
            Feature = feature,
        };

        if (attributes is not null)
        {
            foreach (var (key, value) in attributes) record.Attributes.Set(key, value);
        }

        if (Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity)
        {
            record.TraceId = activity.TraceId.ToHexString();
            record.SpanId = activity.SpanId.ToHexString();
            record.Flags = (int)activity.ActivityTraceFlags;
        }

        return record;
    }
}
