namespace Bit.Belemetry;

/// <summary>Limits applied to what an <see cref="IBelemetrySnapshotProvider"/> returns.</summary>
public sealed class BelemetrySnapshotOptions
{
    /// <summary>The deepest nesting kept; deeper values are replaced with a truncation marker. Defaults to 6.</summary>
    public int MaxDepth { get; set; } = 6;

    /// <summary>The most entries kept of one dictionary or list. Defaults to 50.</summary>
    public int MaxItems { get; set; } = 50;

    /// <summary>The longest string kept, in characters. Defaults to 1024.</summary>
    public int MaxStringLength { get; set; } = 1024;

    /// <summary>The most bytes one provider's snapshot may take (estimated); past it the rest is cut. Defaults to 32 KiB.</summary>
    public int MaxBytes { get; set; } = 32 * 1024;

    /// <summary>How long one provider may take; one that takes longer is reported as timed out. Defaults to 2 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>The most snapshots recorded with <see cref="IBelemetryDiagnostics.RecordSnapshotAsync"/> kept on the timeline. Defaults to 10.</summary>
    public int MaxRecorded { get; set; } = 10;
}
