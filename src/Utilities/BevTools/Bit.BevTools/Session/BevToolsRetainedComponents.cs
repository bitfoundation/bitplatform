namespace Bit.BevTools;

/// <summary>
/// Disposed components of one type that garbage collection did not reclaim: the renderer let go of them, and something
/// else still holds them, and through them whatever they reference. Each is the topmost of a retained subtree: the
/// disposed components it rendered that are still alive too are counted in <see cref="DescendantCount"/>, not reported
/// on their own - a page kept alive keeps every row and icon it rendered, and that is one finding, not forty.
/// </summary>
/// <param name="TypeName">The component type, namespace included.</param>
/// <param name="DisplayName">The type without its namespace.</param>
/// <param name="Count">How many disposed instances of it are alive.</param>
/// <param name="OldestDisposal">When the longest-retained of them was disposed.</param>
/// <param name="FullCollections">Full (generation 2) collections every one of them survived since it was free to go (past its grace period, out of the renderer's buffers).</param>
/// <param name="Causes">
/// References found to the instances that were analysed, and to the retained components they rendered (a cause found on
/// one of those says so); empty when none of the places BevTools looks holds them.
/// </param>
/// <param name="InstanceIds">BevTools' instance ids of the retained instances, then of their retained descendants (at most 50 in all), as the timeline and errors name them.</param>
public sealed record BevToolsRetainedComponents(
    string TypeName,
    string DisplayName,
    int Count,
    DateTimeOffset OldestDisposal,
    int FullCollections,
    IReadOnlyList<BevToolsRetentionCause> Causes,
    IReadOnlyList<long> InstanceIds)
{
    /// <summary>The disposed components these rendered (by the parent BevTools recorded for each) that are still alive with them.</summary>
    public int DescendantCount { get; init; }

    /// <summary><see cref="DescendantCount"/> by type, the most numerous first.</summary>
    public IReadOnlyList<BevToolsRetainedTypeCount> Descendants { get; init; } = [];
}
