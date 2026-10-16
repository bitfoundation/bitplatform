namespace Bit.Belemetry;

/// <summary>
/// The independently consentable parts of Belemetry. Every telemetry record belongs to exactly one feature,
/// and a record is only sent while its feature is both enabled by configuration and granted by the user.
/// </summary>
/// <remarks>
/// The bit values are a wire contract shared with <c>bit-belemetry.js</c> (they cross the interop boundary
/// as plain integers and are persisted in the browser), so they must never be renumbered.
/// </remarks>
[Flags]
public enum BelemetryFeatures
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>JS errors and unhandled rejections, .NET logs at Error/Critical or carrying an exception, and <c>TrackException</c>.</summary>
    Errors = 1,

    /// <summary>Page views - history and enhanced navigation, and <c>TrackPageView</c>.</summary>
    PageViews = 2,

    /// <summary>Clicks and other user interactions.</summary>
    Interactions = 4,

    /// <summary>Web vitals (LCP, INP, CLS, FCP, TTFB), the document-load span, resource timing and long tasks.</summary>
    Performance = 8,

    /// <summary>Browser fetch/XHR client spans.</summary>
    Dependencies = 16,

    /// <summary>A session id that survives page reloads (kept in <c>sessionStorage</c>). Without it the session id lives in memory only.</summary>
    Sessions = 32,

    /// <summary>The pseudonymous user id (<c>enduser.pseudo.id</c>, persisted on the device) and the authenticated user id (<c>user.id</c>).</summary>
    UserTracking = 64,

    /// <summary>.NET logs below Error, and browser console capture below error.</summary>
    Logs = 128,

    /// <summary>.NET <see cref="System.Diagnostics.Activity"/> spans.</summary>
    Traces = 256,

    /// <summary>.NET <see cref="System.Diagnostics.Metrics.Meter"/> metrics.</summary>
    Metrics = 512,

    /// <summary>Keeping unsent telemetry on the device (browser storage or files) to retry it after going offline or restarting.</summary>
    Persistence = 1024,

    /// <summary>Custom events (<c>TrackEvent</c>).</summary>
    Events = 2048,

    /// <summary>Errors, logs, traces and dependencies - what is needed to troubleshoot the app.</summary>
    Diagnostics = Errors | Logs | Traces | Dependencies,

    /// <summary>Page views, interactions, sessions, user tracking and custom events - what describes how the app is used.</summary>
    Analytics = PageViews | Interactions | Sessions | UserTracking | Events,

    /// <summary>Web vitals and metrics - how the app performs.</summary>
    Monitoring = Performance | Metrics,

    /// <summary>Every feature.</summary>
    All = 4095,
}
