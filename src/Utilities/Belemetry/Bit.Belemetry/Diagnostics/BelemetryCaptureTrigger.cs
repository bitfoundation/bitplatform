namespace Bit.Belemetry;

/// <summary>What started a capture.</summary>
/// <param name="Kind">Manual or automatic.</param>
/// <param name="Reason">The reason given (manual), or the trigger that fired (automatic), e.g. <c>unhandled-exception</c>.</param>
/// <param name="EventSequence">The timeline event that triggered it, when one did.</param>
/// <param name="Fingerprint">The fingerprint of the triggering exception, when there is one.</param>
public sealed record BelemetryCaptureTrigger(BelemetryCaptureTriggerKind Kind, string? Reason, long? EventSequence = null, string? Fingerprint = null);
