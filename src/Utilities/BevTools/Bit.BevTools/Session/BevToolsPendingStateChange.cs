namespace Bit.BevTools;

internal sealed record BevToolsPendingStateChange(BevToolsStateChangeInfo Info, string? Detail, DateTimeOffset At, long? TriggerEventId, Dictionary<string, string>? Snapshot, string? Error);
