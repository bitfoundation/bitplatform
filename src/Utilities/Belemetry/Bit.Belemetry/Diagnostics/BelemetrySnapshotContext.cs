namespace Bit.Belemetry;

/// <summary>Why a snapshot is taken, for a provider that describes more or less depending on it.</summary>
public sealed class BelemetrySnapshotContext
{
    /// <summary>The capture's reason, or the label the app recorded the snapshot under.</summary>
    public string? Reason { get; init; }

    /// <summary>Whether it is taken for a capture (rather than recorded on the timeline).</summary>
    public bool ForCapture { get; init; }

    /// <summary>The exception the capture is about, when there is one.</summary>
    public BelemetryDiagnosticException? Exception { get; init; }

    /// <summary>The current route template, when known.</summary>
    public string? Route { get; init; }
}
