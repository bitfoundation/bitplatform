namespace Bit.Belemetry;

/// <summary>The browser script (<c>bit-belemetry.js</c>): what it instruments and how it ships records.</summary>
public sealed class BelemetryBrowserOptions
{
    /// <summary>Whether the browser script runs at all. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How browser records leave the page. Defaults to <see cref="BelemetryBrowserTransport.Auto"/>.</summary>
    public BelemetryBrowserTransport Transport { get; set; } = BelemetryBrowserTransport.Auto;

    /// <summary>Whether URL changes made only to the fragment (<c>#...</c>) count as page views. Defaults to <see langword="false"/>.</summary>
    public bool TrackHashChanges { get; set; }

    /// <summary>The DOM events recorded as interactions. Defaults to <c>click</c>.</summary>
    public List<string> InteractionEvents { get; } = ["click"];

    /// <summary>
    /// Whether the visible text of the clicked element is recorded (truncated to <see cref="MaxInteractionTextLength"/>).
    /// Defaults to <see langword="false"/> because text can contain personal data; prefer <c>data-belemetry-name</c>.
    /// Input values are never read.
    /// </summary>
    public bool CaptureInteractionText { get; set; }

    /// <summary>The maximum length of recorded element text. Defaults to 64.</summary>
    public int MaxInteractionTextLength { get; set; } = 64;

    /// <summary>Whether web vitals (LCP, INP, CLS, FCP, TTFB) are recorded. Defaults to <see langword="true"/>.</summary>
    public bool WebVitals { get; set; } = true;

    /// <summary>Whether the initial document load is recorded as a span with navigation-timing events. Defaults to <see langword="true"/>.</summary>
    public bool DocumentLoad { get; set; } = true;

    /// <summary>Whether every resource fetched by the page (scripts, styles, images) is recorded as a span. Defaults to <see langword="false"/>.</summary>
    public bool ResourceTiming { get; set; }

    /// <summary>Whether main-thread tasks longer than 50 ms are recorded. Defaults to <see langword="false"/>.</summary>
    public bool LongTasks { get; set; }

    /// <summary>Whether <c>fetch</c> calls are recorded as dependency spans. Defaults to <see langword="true"/>.</summary>
    public bool Fetch { get; set; } = true;

    /// <summary>Whether <c>XMLHttpRequest</c> calls are recorded as dependency spans. Defaults to <see langword="true"/>.</summary>
    public bool Xhr { get; set; } = true;

    /// <summary>
    /// Where a W3C <c>traceparent</c> header is added to fetch/XHR calls, so the browser span and the server span
    /// join one trace: <c>same-origin</c>, <c>*</c>, or regular expressions matched against the absolute URL.
    /// Defaults to <c>same-origin</c>. Cross-origin targets must allow the header (CORS).
    /// </summary>
    public List<string> PropagateTraceContextTo { get; } = ["same-origin"];

    /// <summary>Regular expressions of URLs never recorded as dependencies. Defaults to the Blazor framework routes.</summary>
    public List<string> IgnoreUrls { get; } = ["/_framework/", "/_blazor", "/_content/"];

    /// <summary>Regular expressions of JS error messages never recorded.</summary>
    public List<string> IgnoreErrorMessages { get; } = ["ResizeObserver loop"];

    /// <summary>Whether <c>console</c> calls are recorded as logs. Defaults to <see langword="false"/>.</summary>
    public bool CaptureConsole { get; set; }

    /// <summary>The console levels recorded when <see cref="CaptureConsole"/> is on. Defaults to <c>error</c> and <c>warn</c>.</summary>
    public List<string> ConsoleLevels { get; } = ["error", "warn"];

    /// <summary>How long without activity ends a session. Defaults to 30 minutes.</summary>
    public TimeSpan SessionInactivityTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>The maximum length of a session. Defaults to 24 hours.</summary>
    public TimeSpan SessionMaxDuration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>The fraction of sessions whose browser telemetry is recorded (decided once per session). Defaults to 1.0.</summary>
    public double SessionSampleRatio { get; set; } = 1.0;

    /// <summary>The maximum number of records the script holds; the oldest are dropped past it. Defaults to 1024.</summary>
    public int MaxQueueSize { get; set; } = 1024;

    /// <summary>The maximum number of records per request. Defaults to 100.</summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>How often the script exports. Defaults to 5 seconds (it also exports when the page is hidden).</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Whether the script writes its own diagnostics to <c>console.debug</c>. Defaults to <see langword="false"/>.</summary>
    public bool Debug { get; set; }
}
