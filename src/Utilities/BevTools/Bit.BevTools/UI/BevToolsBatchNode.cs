namespace Bit.BevTools;

/// <summary>
/// One row of a batch's cost tree: a component, or same-type siblings that rendered nothing below them (a list's
/// rows), added up so 120 rows read as one line. Times are BuildRenderTree milliseconds in that batch.
/// </summary>
internal sealed record BevToolsBatchNode(long InstanceId, string Name, int Instances, int Depth, int Renders, double SelfMs, double TotalMs);
