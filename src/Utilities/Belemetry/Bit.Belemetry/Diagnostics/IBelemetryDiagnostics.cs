namespace Bit.Belemetry;

/// <summary>
/// Production diagnostics (<see cref="BelemetryOptions.Diagnostics"/>): the diagnostic timeline of this user's app (this
/// circuit's, on Blazor Server), its exception groups, and captures - previewed, narrowed and exported on purpose as
/// support bundles. Inject it (scoped).
/// </summary>
/// <remarks>
/// The usual flow: <see cref="CaptureAsync"/> (or take one of <see cref="PendingCaptures"/>), show the capture's
/// <see cref="BelemetryDiagnosticCapture.Sections"/>, <see cref="BelemetryDiagnosticCapture.Report"/> and
/// <see cref="BelemetryDiagnosticCapture.MissingEvidence"/> to the user, let them narrow it, call
/// <see cref="BelemetryDiagnosticCapture.Confirm"/>, then <see cref="DownloadAsync"/> it or
/// <see cref="BelemetryDiagnosticCapture.ExportAsync(Stream, CancellationToken)"/> it to the app's own support integration.
/// Every call is safe when diagnostics are disabled: nothing is recorded and a capture fails with a reason.
/// </remarks>
public interface IBelemetryDiagnostics
{
    /// <summary>Whether diagnostics are enabled (<see cref="BelemetryDiagnosticsOptions.Enabled"/>).</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Records an event of the app's own on the timeline (a breadcrumb: <c>checkout.submitted</c>). Attributes are the
    /// app's explicit choice, so they are kept - through the sanitizer like everything else.
    /// </summary>
    void Record(string name, IReadOnlyDictionary<string, object?>? attributes = null, BelemetrySeverity severity = BelemetrySeverity.Info, BelemetryDiagnosticCategories category = BelemetryDiagnosticCategories.Custom);

    /// <summary>
    /// Takes the registered snapshot providers' snapshots now and keeps them on the timeline under
    /// <paramref name="label"/> (<c>before-checkout</c>), so a later capture can show the state before and after an operation.
    /// </summary>
    ValueTask RecordSnapshotAsync(string label, CancellationToken cancellationToken = default);

    /// <summary>Makes a capture of what the timeline holds now. It never throws for a diagnostics problem: a failed capture says why.</summary>
    ValueTask<BelemetryDiagnosticCapture> CaptureAsync(BelemetryDiagnosticCaptureOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>The timeline's events now (the .NET side; the browser's are fetched by a capture).</summary>
    IReadOnlyList<BelemetryDiagnosticEvent> GetEvents();

    /// <summary>The exception groups, most recently seen first.</summary>
    IReadOnlyList<BelemetryExceptionGroup> GetExceptionGroups();

    /// <summary>The automatic captures waiting for review (<see cref="BelemetryDiagnosticsOptions.AutoCaptureTriggers"/>).</summary>
    IReadOnlyList<BelemetryDiagnosticCapture> PendingCaptures { get; }

    /// <summary>Removes a capture from <see cref="PendingCaptures"/> (reviewed, exported or declined).</summary>
    bool Dismiss(BelemetryDiagnosticCapture capture);

    /// <summary>
    /// Raised when an automatic capture is ready for review - e.g. to offer "Something went wrong. Send a report?". Raised
    /// outside any render: marshal to the UI (<c>InvokeAsync(StateHasChanged)</c>).
    /// </summary>
    event Action<BelemetryDiagnosticCapture>? CaptureAvailable;

    /// <summary>
    /// Saves a confirmed capture's bundle in the browser (a download of <see cref="BelemetryDiagnosticCapture.SuggestedFileName"/>).
    /// Returns false when there is no browser to save it in (prerendering, a disconnected circuit) - export it with
    /// <see cref="BelemetryDiagnosticCapture.ExportAsync(Stream, CancellationToken)"/> instead, as a Hybrid app would to a file.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the capture is not confirmed.</exception>
    ValueTask<bool> DownloadAsync(BelemetryDiagnosticCapture capture, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets everything the timeline holds - the .NET side and the page's, with the copy the page keeps for its next
    /// load - (the user asked to, or a support case was closed).
    /// </summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
