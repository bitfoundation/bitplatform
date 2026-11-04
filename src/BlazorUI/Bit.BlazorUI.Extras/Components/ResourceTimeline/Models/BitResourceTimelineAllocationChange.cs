namespace Bit.BlazorUI;

/// <summary>
/// A pending move, resize or create, handed to
/// <c>OnAllocationChanging</c> before it is committed. <see cref="Allocation"/>
/// is the same instance supplied in the config, already updated with the
/// previewed resource and times. Return <c>false</c> from the callback to
/// restore <see cref="PreviousResourceId"/> / <see cref="PreviousStartTime"/> /
/// <see cref="PreviousEndTime"/> without a reload.
/// </summary>
public sealed class BitResourceTimelineAllocationChange
{
    /// <summary>
    /// The allocation being changed, already updated to the previewed
    /// resource and times. The same instance supplied in the config.
    /// </summary>
    public required BitResourceTimelineAllocation Allocation { get; init; }

    /// <summary>Resource the allocation belonged to before this edit.</summary>
    public required string PreviousResourceId { get; init; }

    /// <summary>Start time before this edit.</summary>
    public DateTimeOffset PreviousStartTime { get; init; }

    /// <summary>End time before this edit.</summary>
    public DateTimeOffset PreviousEndTime { get; init; }

    /// <summary>Whether this is a move, a resize or a create.</summary>
    public BitResourceTimelineAllocationChangeKind Kind { get; init; }
}
