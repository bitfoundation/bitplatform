namespace Bit.Belemetry;

/// <summary>What happened to a request the sender tried to deliver.</summary>
internal enum BelemetrySendOutcome
{
    /// <summary>Accepted by the backend (possibly after retrying part of it).</summary>
    Delivered,

    /// <summary>Refused for good (a non-retryable status) - retrying would not help.</summary>
    Rejected,

    /// <summary>Not delivered for a transient reason (network, throttling, server error) - worth retrying later.</summary>
    Failed,

    /// <summary>
    /// Given up because consent for a feature of its records was revoked after it was built (<see cref="BelemetryConsentMark"/>):
    /// it may hold that feature's records, so it is neither sent again nor stored.
    /// </summary>
    Withdrawn,
}
