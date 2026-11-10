namespace Bit.Belemetry;

/// <summary>What became of one snapshot provider.</summary>
public enum BelemetrySnapshotStatus
{
    /// <summary>It returned data, sanitized as reported.</summary>
    Captured,

    /// <summary>It returned nothing.</summary>
    Empty,

    /// <summary>It threw, or its task failed, within the timeout; its data is not included and only the exception's type is reported.</summary>
    Failed,

    /// <summary>
    /// It did not finish within <see cref="BelemetrySnapshotOptions.Timeout"/>: still running then, given up at its
    /// cancellation token, or returned only after it (a provider that blocks); its data is not included.
    /// </summary>
    TimedOut,

    /// <summary>The user or the app left it out of the bundle.</summary>
    Excluded,
}
