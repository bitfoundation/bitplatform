namespace Bit.BevTools;

/// <summary>One render on a batch's time axis: when it started after the batch's first render, how long it took, how deep it is.</summary>
internal sealed record BevToolsTimelineBar(long InstanceId, string Name, int Depth, double StartMs, double DurationMs);
