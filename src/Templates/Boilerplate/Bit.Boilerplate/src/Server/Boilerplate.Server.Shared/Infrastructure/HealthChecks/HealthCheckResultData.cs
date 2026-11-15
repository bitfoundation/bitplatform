namespace Boilerplate.Server.Shared.Infrastructure.HealthChecks;

/// <summary>
/// The data keys of a reused result's own run (See CachedHealthCheck), which a check can only pass on in its result's
/// data. The health report writer reports them as the entry's <c>Duration</c>, <c>VerifiedAt</c> and
/// <c>CacheDuration</c> rather than as data (See WriteHealthReport), so no reader has to know these keys.
/// </summary>
public static class HealthCheckResultData
{
    /// <summary>How long the run took, a <see cref="TimeSpan"/>.</summary>
    public const string Duration = "Duration";

    /// <summary>When the run finished, a <see cref="DateTimeOffset"/>.</summary>
    public const string VerifiedAt = "VerifiedAt";

    /// <summary>How long a healthy result is reused, a <see cref="TimeSpan"/>.</summary>
    public const string CacheDuration = "CacheDuration";

    public static readonly string[] Keys = [Duration, VerifiedAt, CacheDuration];
}
