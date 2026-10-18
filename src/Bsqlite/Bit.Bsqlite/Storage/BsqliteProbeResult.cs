namespace Bit.Bsqlite.Storage;

/// <summary>
/// What a backend discovered about the current browser. Probing happens once per session and the
/// result is surfaced verbatim through diagnostics, so that a support ticket can be answered from
/// the report alone.
/// </summary>
/// <remarks>
/// Constructed through <see cref="Available"/> and <see cref="Unavailable"/> so that an unavailable
/// result always carries a reason - a probe that fails without saying why is the thing that makes
/// these bugs unanswerable.
/// </remarks>
public sealed record BsqliteProbeResult
{
    private BsqliteProbeResult()
    {
    }

    /// <summary>Whether the backend can be used here.</summary>
    public bool IsAvailable { get; private init; }

    /// <summary>
    /// Why the backend is unusable. Always present when <see cref="IsAvailable"/> is false, and
    /// always absent when it is true.
    /// </summary>
    public string? UnavailableReason { get; private init; }

    /// <summary>Storage the origin may still use, when the browser will say.</summary>
    public long? QuotaBytes { get; private init; }

    /// <summary>Storage the origin has already used, when the browser will say.</summary>
    public long? UsageBytes { get; private init; }

    /// <summary>
    /// The environment facts the verdict rests on - for example <c>crossOriginIsolated</c>,
    /// <c>navigator.storage.getDirectory</c>, or JSPI support. Reported as observed, without
    /// interpretation.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; private init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Reports a usable backend.</summary>
    public static BsqliteProbeResult Available(
        long? quotaBytes = null,
        long? usageBytes = null,
        IReadOnlyDictionary<string, string>? environment = null)
        => new()
        {
            IsAvailable = true,
            QuotaBytes = quotaBytes,
            UsageBytes = usageBytes,
            Environment = Freeze(environment),
        };

    /// <summary>Reports an unusable backend and why.</summary>
    /// <param name="reason">
    /// Written for whoever reads the error, naming the missing capability rather than restating that
    /// something failed.
    /// </param>
    /// <param name="environment">The facts behind the verdict.</param>
    public static BsqliteProbeResult Unavailable(
        string reason,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new BsqliteProbeResult
        {
            IsAvailable = false,
            UnavailableReason = reason,
            Environment = Freeze(environment),
        };
    }

    private static IReadOnlyDictionary<string, string> Freeze(
        IReadOnlyDictionary<string, string>? environment)
        => environment is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(environment, StringComparer.Ordinal);
}
