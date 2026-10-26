namespace Bit.Belemetry;

/// <summary>What became of a request <see cref="BelemetryHttpSender"/> sent.</summary>
/// <param name="Outcome">How the last attempt ended.</param>
/// <param name="Remaining">
/// What is still undelivered: the request itself, or after a partial success only the items to retry - which is what
/// must be stored for a later retry, since the accepted ones would otherwise be sent twice.
/// </param>
/// <param name="Delivered">The records the backend accepted, for the self-telemetry.</param>
/// <param name="Rejected">
/// The records the backend refused for good, a partial success's included: not retried, and not delivered either.
/// </param>
internal readonly record struct BelemetrySendResult(BelemetrySendOutcome Outcome, BelemetryHttpRequest Remaining, int Delivered, int Rejected);
