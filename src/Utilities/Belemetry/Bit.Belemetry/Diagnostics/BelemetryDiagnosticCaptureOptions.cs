namespace Bit.Belemetry;

/// <summary>What a capture includes. Every section is still subject to consent, the sanitizer and the size limits.</summary>
public sealed class BelemetryDiagnosticCaptureOptions
{
    /// <summary>Why the capture is made (<c>user-report</c>, <c>support-ticket</c>); short, low-cardinality text.</summary>
    public string? Reason { get; set; }

    /// <summary>
    /// What the user says happened. Kept as user-provided text, marked as such in the report, and passed through the
    /// privacy patterns; it is shown in the preview so the user can see what they are sending.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The exception the capture is about, when the app has one in hand (an <c>ErrorBoundary</c>'s
    /// <c>CurrentException</c>, a caught exception). It is sanitized like any other and becomes the report's subject.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>Whether the timeline's successful activity is included, not only its failures. Defaults to <see langword="true"/>.</summary>
    public bool IncludeRecentActivity { get; set; } = true;

    /// <summary>
    /// Only the events of the last this-long are included (the failures and the exception groups are kept whatever their
    /// age). Defaults to null: the whole timeline.
    /// </summary>
    public TimeSpan? Lookback { get; set; }

    /// <summary>Whether the browser script's timeline (page views, fetch/XHR, JS errors, reconnection) is included. Defaults to <see langword="true"/>.</summary>
    public bool IncludeBrowserEvidence { get; set; } = true;

    /// <summary>Whether the components the timeline names are described (and an <see cref="IBelemetryComponentContextProvider"/> asked). Defaults to <see langword="true"/>.</summary>
    public bool IncludeComponentContext { get; set; } = true;

    /// <summary>
    /// Whether the application's <see cref="IBelemetrySnapshotProvider"/>s are asked for snapshots, and the snapshots
    /// recorded on the timeline included. Defaults to <see langword="false"/>: application state is opt-in per capture.
    /// </summary>
    public bool IncludeSnapshots { get; set; }

    /// <summary>The snapshot providers to ask, by name; null asks them all.</summary>
    public IReadOnlyCollection<string>? SnapshotProviders { get; set; }
}
