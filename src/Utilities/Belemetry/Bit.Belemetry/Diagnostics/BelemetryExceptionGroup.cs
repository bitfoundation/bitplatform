namespace Bit.Belemetry;

/// <summary>
/// The occurrences of one failure, grouped by <see cref="BelemetryExceptionFingerprint"/>: how many, since when, in
/// which releases, and the most recent occurrences with their own evidence.
/// </summary>
public sealed class BelemetryExceptionGroup
{
    /// <summary>The fingerprint.</summary>
    public required string Fingerprint { get; init; }

    /// <summary>The <see cref="BelemetryExceptionFingerprint.Version"/> that made <see cref="Fingerprint"/>, or <c>custom</c> for an application override.</summary>
    public required string FingerprintVersion { get; init; }

    /// <summary>The exception type.</summary>
    public required string ExceptionType { get; init; }

    /// <summary>The message template.</summary>
    public string? MessageTemplate { get; init; }

    /// <summary>The frame grouping anchored on.</summary>
    public string? Anchor { get; init; }

    /// <summary>The application's classification, when an <see cref="IBelemetryExceptionClassifier"/> gave one.</summary>
    public string? Classification { get; init; }

    /// <summary>How many times it happened since the timeline started (duplicates the error limits suppressed included).</summary>
    public int Count { get; init; }

    /// <summary>The first occurrence.</summary>
    public DateTimeOffset FirstSeen { get; init; }

    /// <summary>The last occurrence.</summary>
    public DateTimeOffset LastSeen { get; init; }

    /// <summary>The releases it happened in (at most ten).</summary>
    public IReadOnlyList<string> Releases { get; init; } = [];

    /// <summary>The most recent occurrences (at most five), newest last.</summary>
    public IReadOnlyList<BelemetryExceptionOccurrence> Occurrences { get; init; } = [];
}
