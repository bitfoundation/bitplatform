namespace Bit.Belemetry;

/// <summary>
/// A diagnostic capture: the sanitized evidence of one failure (or one user report), ready to be previewed, narrowed
/// and exported as a support bundle. Made by <see cref="IBelemetryDiagnostics.CaptureAsync"/> or by an automatic
/// trigger; it lives in memory only, and nothing of it leaves the process until <see cref="ExportAsync(Stream, CancellationToken)"/>,
/// which requires <see cref="Confirm"/> first.
/// </summary>
/// <remarks>
/// The preview operations (<see cref="ExcludeSection"/>, <see cref="ExcludeEvent"/>, <see cref="RedactAttribute"/>,
/// <see cref="ExcludeSnapshot"/>, <see cref="SetDescription"/>) narrow what is exported and are recorded in the manifest;
/// each one withdraws a previous confirmation, so what is confirmed is always what is exported. The instance is not
/// thread-safe: preview it from one place (a dialog).
/// </remarks>
public sealed class BelemetryDiagnosticCapture
{
    private readonly List<BelemetryDiagnosticEvent> _events;
    private readonly HashSet<BelemetryDiagnosticEvent> _excludedEvents = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<string> _redactedKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _excludedSnapshots = new(StringComparer.Ordinal);
    private readonly List<string> _userChanges = [];
    private readonly BelemetryCaptureContent _content;
    private BelemetryIssueReport? _report;
    private BelemetryFeatures _reportGrant;
    private string? _description;

    internal BelemetryDiagnosticCapture(BelemetryCaptureContent content)
    {
        _content = content;
        _events = content.Events;
        _description = content.Description;
        Id = content.Id;
        CreatedAt = content.CreatedAt;
        Trigger = content.Trigger;
        Succeeded = true;
    }

    private BelemetryDiagnosticCapture(string id, DateTimeOffset createdAt, BelemetryCaptureTrigger trigger, string error)
    {
        _content = new BelemetryCaptureContent { Id = id, CreatedAt = createdAt, Trigger = trigger, Options = new BelemetryDiagnosticsOptions() };
        _events = [];
        Id = id;
        CreatedAt = createdAt;
        Trigger = trigger;
        Error = error;
    }

    internal static BelemetryDiagnosticCapture Failed(BelemetryCaptureTrigger trigger, string error) =>
        new(BelemetryCaptureContent.NewId(), DateTimeOffset.UtcNow, trigger, error);

    /// <summary>A random id (16 hexadecimal characters), unrelated to any user, session or trace.</summary>
    public string Id { get; }

    /// <summary>When it was made.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>What started it.</summary>
    public BelemetryCaptureTrigger Trigger { get; }

    /// <summary>Whether it was made; when not, <see cref="Error"/> says why and nothing else is set.</summary>
    public bool Succeeded { get; }

    /// <summary>Why it could not be made (diagnostics disabled, ...).</summary>
    public string? Error { get; }

    /// <summary>The sections the export will hold now.</summary>
    public BelemetryBundleSections IncludedSections { get; private set; } = BelemetryBundleSections.All;

    /// <summary>The release, environment and runtime (null when the section is excluded).</summary>
    public BelemetryReleaseInfo? Release => Includes(BelemetryBundleSections.Release) ? _content.Release : null;

    /// <summary>The timeline events the export will hold, ordered by time.</summary>
    public IReadOnlyList<BelemetryDiagnosticEvent> Events => _events.Where(IsExported).ToList();

    /// <summary>The exception the capture is about, when there is one: the one the app passed, else the trigger's, else the latest.</summary>
    public BelemetryDiagnosticEvent? Subject => _content.Subject is { } subject && IsExported(subject) ? subject : null;

    /// <summary>
    /// The exception groups the export will hold: without the occurrences of the events the user removed (or left out
    /// with their section), and without a group that has none left - nor any while the user does not grant Errors.
    /// </summary>
    public IReadOnlyList<BelemetryExceptionGroup> ExceptionGroups
    {
        get
        {
            if (Includes(BelemetryBundleSections.Exceptions) is false || MayCapture(BelemetryFeatures.Errors) is false) return [];

            var removed = new HashSet<(string, long, bool)>();
            foreach (var e in _events)
            {
                if (e.Exception is { } x && IsExported(e) is false) removed.Add((x.Fingerprint, e.Sequence, e.Source == BelemetryDiagnosticSource.Browser));
            }

            var groups = new List<BelemetryExceptionGroup>(_content.Groups.Count);
            foreach (var group in _content.Groups)
            {
                if (Includes(BelemetryBundleSections.Browser) is false && _content.BrowserGroups.Contains(group.Fingerprint)) continue;
                if (removed.Count == 0)
                {
                    groups.Add(group);
                    continue;
                }

                var occurrences = group.Occurrences.Where(o => o.Sequence is not { } sequence || removed.Contains((group.Fingerprint, sequence, o.Origin == "browser")) is false).ToList();
                if (occurrences.Count == 0) continue;
                groups.Add(occurrences.Count == group.Occurrences.Count ? group : new BelemetryExceptionGroup
                {
                    Fingerprint = group.Fingerprint,
                    FingerprintVersion = group.FingerprintVersion,
                    ExceptionType = group.ExceptionType,
                    MessageTemplate = group.MessageTemplate,
                    Anchor = group.Anchor,
                    Classification = group.Classification,
                    Count = group.Count,
                    FirstSeen = group.FirstSeen,
                    LastSeen = group.LastSeen,
                    Releases = group.Releases,
                    Occurrences = occurrences,
                });
            }
            return groups;
        }
    }

    /// <summary>The components the export will describe.</summary>
    public IReadOnlyList<BelemetryComponentInfo> Components => Includes(BelemetryBundleSections.Components) ? _content.Components : [];

    /// <summary>The snapshots the export will hold (an excluded one is listed with <see cref="BelemetrySnapshotStatus.Excluded"/> and no data).</summary>
    public IReadOnlyList<BelemetryDiagnosticSnapshot> Snapshots => Includes(BelemetryBundleSections.Snapshots)
        ? _content.Snapshots.Select(s => _excludedSnapshots.Contains(s.Name) ? s.WithStatus(BelemetrySnapshotStatus.Excluded) : s).ToList()
        : [];

    /// <summary>What the user said happened (null when the section is excluded).</summary>
    public string? Description => Includes(BelemetryBundleSections.Description) ? _description : null;

    /// <summary>What each section holds, for the preview: counts, omissions and why.</summary>
    public IReadOnlyList<BelemetryBundleSection> Sections => BelemetryCaptureSummary.Sections(this, _content);

    /// <summary>What evidence is missing, and why: unavailable sources, consent, limits, the user's exclusions.</summary>
    public IReadOnlyList<string> MissingEvidence => BelemetryCaptureSummary.Missing(this, _content);

    /// <summary>Events and values the timelines dropped before the capture (by reason), and what the capture left out.</summary>
    public IReadOnlyDictionary<string, int> Dropped => _content.Dropped;

    /// <summary>The attribute keys the user redacted in the preview.</summary>
    public IReadOnlyCollection<string> RedactedAttributes => _redactedKeys;

    /// <summary>What the user changed in the preview, in order (recorded in the manifest).</summary>
    public IReadOnlyList<string> UserChanges => _userChanges;

    /// <summary>Whether the content was confirmed for export, and has not changed since.</summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>The issue report, rebuilt from what the export holds now (after a preview change, or a change of consent).</summary>
    public BelemetryIssueReport Report
    {
        get
        {
            var grant = _content.CurrentGrant?.Invoke() ?? BelemetryFeatures.All;
            if (_report is null || grant != _reportGrant)
            {
                _reportGrant = grant;
                _report = BelemetryIssueReportBuilder.Build(this, _content);
            }
            return _report;
        }
    }

    internal BelemetryCaptureContent Content => _content;

    internal ISet<string> RedactedKeys => _redactedKeys;

    /// <summary>Leaves a section out of the export.</summary>
    public void ExcludeSection(BelemetryBundleSections section)
    {
        EnsureSucceeded();
        if ((IncludedSections & section) == 0) return;
        IncludedSections &= ~section;
        Changed($"excluded section {section}");
    }

    /// <summary>Puts a section the user excluded back.</summary>
    public void IncludeSection(BelemetryBundleSections section)
    {
        EnsureSucceeded();
        if ((IncludedSections & section) == section) return;
        IncludedSections |= section & BelemetryBundleSections.All;
        Changed($"included section {section}");
    }

    /// <summary>Leaves one event out of the export.</summary>
    public bool ExcludeEvent(BelemetryDiagnosticEvent diagnosticEvent)
    {
        EnsureSucceeded();
        ArgumentNullException.ThrowIfNull(diagnosticEvent);
        if (_events.Contains(diagnosticEvent) is false || _excludedEvents.Add(diagnosticEvent) is false) return false;
        Changed($"excluded event {BelemetryDiagnosticJson.EventId(diagnosticEvent)}");
        return true;
    }

    /// <summary>Replaces every value of an attribute key with <c>REDACTED</c> in the export.</summary>
    public void RedactAttribute(string key)
    {
        EnsureSucceeded();
        ArgumentException.ThrowIfNullOrEmpty(key);
        if (_redactedKeys.Add(key)) Changed($"redacted attribute {key}");
    }

    /// <summary>Leaves one snapshot's data out of the export.</summary>
    public void ExcludeSnapshot(string name)
    {
        EnsureSucceeded();
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (_excludedSnapshots.Add(name)) Changed($"excluded snapshot {name}");
    }

    /// <summary>Sets what the user says happened (user-provided text, through the privacy patterns); null removes it.</summary>
    public void SetDescription(string? description)
    {
        EnsureSucceeded();
        var counters = new BelemetrySanitizeCounters();
        var sanitized = description is null ? null : _content.Sanitizer?.Text(description, 4000, ref counters) ?? BelemetryValueLimit.Truncate(description, 4000);
        if (string.Equals(sanitized, _description, StringComparison.Ordinal)) return;
        _description = string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
        Changed(_description is null ? "removed description" : "set description");
    }

    /// <summary>Confirms the current content for export: the user (or the support agent) has seen it and agrees to share it.</summary>
    public void Confirm()
    {
        EnsureSucceeded();
        IsConfirmed = true;
    }

    /// <summary>
    /// Writes the support bundle (a ZIP of JSON documents and the Markdown report) to <paramref name="destination"/>.
    /// It is built in memory first and written in one piece, so a failed or cancelled export writes nothing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the capture failed, or was not confirmed since its last change.</exception>
    public async Task ExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var bytes = await ExportAsync(cancellationToken);
        await destination.WriteAsync(bytes, cancellationToken);
        await destination.FlushAsync(cancellationToken);
    }

    /// <summary>The support bundle's bytes.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the capture failed, or was not confirmed since its last change.</exception>
    public Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
    {
        EnsureSucceeded();
        if (IsConfirmed is false) throw new InvalidOperationException("Confirm() the capture after previewing it: a support bundle is only exported on purpose.");
        return Task.FromResult(BelemetrySupportBundle.Write(this, cancellationToken));
    }

    /// <summary>The suggested file name: <c>{service}-diagnostics-{yyyyMMdd-HHmmss}-{id}.zip</c>.</summary>
    public string SuggestedFileName
    {
        get
        {
            var service = new string((_content.Release?.ServiceName ?? "app").Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray());
            if (service.Length > 40) service = service[..40];
            return $"{service}-diagnostics-{CreatedAt.ToUniversalTime():yyyyMMdd-HHmmss}-{Id}.zip";
        }
    }

    /// <summary>The issue report as Markdown.</summary>
    public string ToMarkdown() => Report.ToMarkdown();

    internal bool Includes(BelemetryBundleSections section) => (IncludedSections & section) == section;

    internal bool IsExported(BelemetryDiagnosticEvent e)
    {
        if (_excludedEvents.Contains(e)) return false;

        // Consent is checked again now: a capture open in the preview while the user revokes a feature loses its events.
        var check = e.Source == BelemetryDiagnosticSource.Browser ? _content.MayCaptureBrowser : _content.MayCapture;
        if (e.Feature != BelemetryFeatures.None && check is not null && check(e.Feature) is false) return false;
        return e.Source == BelemetryDiagnosticSource.Browser ? Includes(BelemetryBundleSections.Browser) : Includes(BelemetryBundleSections.Timeline);
    }

    internal bool IsSnapshotExcluded(string name) => _excludedSnapshots.Contains(name);

    private bool MayCapture(BelemetryFeatures feature) => feature == BelemetryFeatures.None || _content.MayCapture is not { } check || check(feature);

    /// <summary>An event's attribute as the export holds it: <c>REDACTED</c> when the user redacted its key in the preview.</summary>
    internal string? Attribute(BelemetryDiagnosticEvent e, string key)
    {
        if (e.Attributes.TryGetValue(key, out var value) is false || value is null) return null;
        return _redactedKeys.Contains(key) ? BelemetryDiagnosticSanitizer.Redacted : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private void Changed(string change)
    {
        _userChanges.Add(change);
        _report = null;
        IsConfirmed = false;
    }

    private void EnsureSucceeded()
    {
        if (Succeeded is false) throw new InvalidOperationException($"The capture failed: {Error}");
    }
}
