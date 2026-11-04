namespace Bit.Belemetry;

/// <summary>What became of one snapshot provider.</summary>
public enum BelemetrySnapshotStatus
{
    /// <summary>It returned data, sanitized as reported.</summary>
    Captured,

    /// <summary>It returned nothing.</summary>
    Empty,

    /// <summary>It threw; its data is not included.</summary>
    Failed,

    /// <summary>It took longer than <see cref="BelemetrySnapshotOptions.Timeout"/>; its data is not included.</summary>
    TimedOut,

    /// <summary>The user or the app left it out of the bundle.</summary>
    Excluded,
}
