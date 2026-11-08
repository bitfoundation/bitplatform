namespace Bit.BlazorUI;

/// <summary>
/// A proposed new allocation from a drag on empty content.
/// The host must assign <see cref="BitResourceTimelineAllocation.Id"/> and
/// return the full bar, or return <c>null</c> to cancel.
/// </summary>
public sealed class BitResourceTimelineCreateRequest
{
    /// <summary>Resource row the new bar should belong to.</summary>
    public required string ResourceId { get; init; }

    /// <summary>Snapped start of the drawn range.</summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>Snapped end of the drawn range (at least <c>EditMinDurationMinutes</c>).</summary>
    public DateTimeOffset EndTime { get; init; }
}
