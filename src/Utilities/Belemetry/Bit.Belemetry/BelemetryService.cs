using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Bit.Belemetry;

/// <summary>The scoped <see cref="IBelemetry"/>.</summary>
/// <remarks>
/// The scope's <see cref="NavigationManager"/>, when the host has one (a host without Blazor registers none), gives the
/// page views .NET records itself their current page and base path.
/// </remarks>
internal sealed class BelemetryService(BelemetrySettings settings, BelemetryPipeline pipeline, BelemetryConsentState consent, BelemetryJsBridge bridge, NavigationManager? navigation = null) : IBelemetry
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
        Enqueue(record);
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
        var template = record.Attributes.GetString(BelemetrySemanticConventions.UrlTemplate) is { Length: > 0 } given ? given : null;
        // The current page when no URL is given, as the script takes location.href.
        var baseUri = Navigation(n => n.BaseUri);
        url ??= Navigation(n => n.Uri);
        if (url is not null)
        {
            record.Attributes.Set(BelemetrySemanticConventions.UrlFull, url);
            if (PathOf(url, baseUri) is { } path)
            {
                record.Attributes.Set(BelemetrySemanticConventions.UrlPath, path);
                // One name for all the views of a page whatever its ids, as the script's page views have: the caller's
                // template, else the app's route the path matches below the base path, else the path with its ids replaced.
                if (template is null)
                {
                    template = settings.PageRouteMatcher.Template(path, BelemetryPageRouteMatcher.BasePath(baseUri));
                    record.Attributes.Set(BelemetrySemanticConventions.UrlTemplate, template);
                    pipeline.WarnIfNoPageRoutes();
                }
            }
        }
        // The page view's name, as the script writes it: the name given, else the template.
        record.Body = name is { Length: > 0 } ? name : template;
        Enqueue(record);
    }

    /// <summary>
    /// The path of a page view's URL: an absolute URL's, a root-relative one's, or a relative one's below the base URI
    /// (<c>orders/42</c> under <c>/shop/</c> is <c>/shop/orders/42</c>), as the browser resolves it.
    /// </summary>
    private static string? PathOf(string url, string? baseUri)
    {
        // Before Uri: on Unix "/orders/42" is an absolute file URI.
        if (url.StartsWith('/') && url.StartsWith("//", StringComparison.Ordinal) is false) return url.Split('?', '#')[0];
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)) return absolute.AbsolutePath;
        var root = baseUri is not null && Uri.TryCreate(baseUri, UriKind.Absolute, out var b) ? b : new Uri("http://localhost/");
        return Uri.TryCreate(root, url, out var resolved) ? resolved.AbsolutePath : null;
    }

    /// <summary>A value of the scope's <see cref="NavigationManager"/>; null without one, or outside a page (not initialized).</summary>
    private string? Navigation(Func<NavigationManager, string> read)
    {
        if (navigation is null) return null;
        try
        {
            return read(navigation);
        }
        catch (Exception)
        {
            return null;
        }
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
        record.Exception = exception;
        Enqueue(record);
    }

    /// <summary>Telemetry never fails the code that records it: past argument validation, a failure is a diagnostic.</summary>
    private void Enqueue(BelemetryLogRecord record)
    {
        try
        {
            pipeline.Enqueue(record);
        }
        catch (Exception ex)
        {
            pipeline.LogDiagnostic(ex, "Recording a manual event failed; it is dropped.");
        }
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
