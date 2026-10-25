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

    /// <summary>
    /// Whether the time spent on each page is recorded, as a <c>browser.page_visit_time</c> event carrying
    /// <c>page.visit.duration</c> (milliseconds) when the next page view starts or the page is hidden (Application
    /// Insights shows it as the <c>PageVisitTime</c> event). Defaults to <see langword="false"/>.
    /// </summary>
    public bool TrackPageVisitTime { get; set; }

    /// <summary>The DOM events recorded as interactions. Defaults to <c>click</c>.</summary>
    public List<string> InteractionEvents { get; } = ["click"];

    /// <summary>
    /// Whether the visible text of the clicked element is recorded (truncated to <see cref="MaxInteractionTextLength"/>).
    /// Defaults to <see langword="false"/> because text can contain personal data; prefer <c>data-bit-belemetry-name</c>.
    /// Input values are never read.
    /// </summary>
    public bool CaptureInteractionText { get; set; }

    /// <summary>The maximum length of recorded element text. Defaults to 64.</summary>
    public int MaxInteractionTextLength { get; set; } = 64;

    /// <summary>
    /// Whether three clicks on one element within a second also emit a <c>browser.rage_click</c> event (feature
    /// <see cref="BelemetryFeatures.Interactions"/>) - the frustration signal of a user who expected something to
    /// happen. Once per burst; the clicks themselves are still recorded. Defaults to <see langword="true"/>.
    /// </summary>
    public bool TrackRageClicks { get; set; } = true;

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

    /// <summary>
    /// Regular expressions of JS error messages never recorded. Defaults to the benign <c>ResizeObserver loop</c>
    /// warnings and to <c>Script error.</c> - what the browser reports, with no stack or location, for an error in a
    /// cross-origin script loaded without CORS: nothing in it can be acted on.
    /// </summary>
    public List<string> IgnoreErrorMessages { get; } = ["ResizeObserver loop", @"^Script error\.?$"];

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

    /// <summary>
    /// On Blazor Server with the <see cref="BelemetryBrowserTransport.Relay"/> transport, the most records one circuit
    /// may relay per minute on average (at least 1); the excess is dropped. The page is untrusted there and the queue it
    /// relays into serves every user. On top of it a circuit may send a burst of <see cref="MaxQueueSize"/> plus
    /// <see cref="BelemetryConsentOptions.PendingLimit"/> records - a full queue and what a consent decision releases.
    /// Defaults to 3000.
    /// </summary>
    public int MaxRelayedPerMinute { get; set; } = 3000;

    /// <summary>Whether the script writes its own diagnostics to <c>console.debug</c>. Defaults to <see langword="false"/>.</summary>
    public bool Debug { get; set; }
}
