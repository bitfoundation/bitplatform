using System.Security.Cryptography;

namespace Bit.Belemetry;

/// <summary>Everything a capture was built from, before the preview narrows it.</summary>
internal sealed class BelemetryCaptureContent
{
    public required string Id { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required BelemetryCaptureTrigger Trigger { get; init; }

    public required BelemetryDiagnosticsOptions Options { get; init; }

    public BelemetryDiagnosticSanitizer? Sanitizer { get; init; }

    public BelemetryHostKind Host { get; init; }

    public string? Description { get; set; }

    public BelemetryReleaseInfo? Release { get; set; }

    public List<BelemetryDiagnosticEvent> Events { get; set; } = [];

    public BelemetryDiagnosticEvent? Subject { get; set; }

    public List<BelemetryExceptionGroup> Groups { get; set; } = [];

    public List<BelemetryComponentInfo> Components { get; set; } = [];

    public List<BelemetryDiagnosticSnapshot> Snapshots { get; set; } = [];

    public Dictionary<string, int> Dropped { get; } = new(StringComparer.Ordinal);

    /// <summary>Evidence that could not be collected, with the reason (shown as missing evidence).</summary>
    public List<string> Unavailable { get; } = [];

    public bool SnapshotsRequested { get; set; }

    public bool BrowserRequested { get; set; }

    public bool BrowserAvailable { get; set; }

    public string? ComponentProvider { get; set; }

    public BelemetryFeatures Granted { get; set; }

    public bool ConsentDecided { get; set; }

    public int ConsentOmitted { get; set; }

    public int LimitOmitted { get; set; }

    public int LookbackOmitted { get; set; }

    /// <summary>Whether the user's consent lets a capture hold a feature's events now: checked again at every read and export.</summary>
    public Func<BelemetryFeatures, bool>? MayCapture { get; init; }

    /// <summary>
    /// Whether the page's own consent lets a capture hold a browser event's feature now. The page governs its events on
    /// every host - on a server the operator's <see cref="BelemetryServerOptions.Features"/> govern the server's own.
    /// </summary>
    public Func<BelemetryFeatures, bool>? MayCaptureBrowser { get; init; }

    /// <summary>The user's grant now, to rebuild the report when it changed.</summary>
    public Func<BelemetryFeatures>? CurrentGrant { get; init; }

    /// <summary>The fingerprints of the groups made from browser events only (left out with the Browser section).</summary>
    public HashSet<string> BrowserGroups { get; } = new(StringComparer.Ordinal);

    public void AddDropped(string reason, int count)
    {
        if (count > 0) Dropped[reason] = Dropped.TryGetValue(reason, out var n) ? n + count : count;
    }

    public static string NewId() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));
}
