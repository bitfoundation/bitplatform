namespace Bit.Belemetry;

/// <summary>What an <see cref="IBelemetryExceptionClassifier"/> decided about an exception.</summary>
public sealed class BelemetryExceptionClassification
{
    /// <summary>A short, low-cardinality classification (<c>network</c>, <c>validation</c>, <c>concurrency</c>), shown with the group and part of the fingerprint.</summary>
    public string? Category { get; init; }

    /// <summary>
    /// A grouping key that replaces the computed fingerprint: exceptions with the same key are one group whatever
    /// their stack. It is hashed (<see cref="BelemetryExceptionFingerprint.FromKey"/>), never exported as it is.
    /// </summary>
    public string? Fingerprint { get; init; }

    /// <summary>Whether it is critical: <see cref="BelemetryDiagnosticTriggers.Critical"/> captures it automatically.</summary>
    public bool Critical { get; init; }

    /// <summary>Whether to leave it off the timeline and out of the groups altogether (an expected, noisy failure).</summary>
    public bool Ignore { get; init; }
}
