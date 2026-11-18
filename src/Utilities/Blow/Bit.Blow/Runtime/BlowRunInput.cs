namespace Bit.Blow;

/// <summary>A value arriving at a node along one of its incoming edges.</summary>
/// <param name="Edge">The edge it came along, as it was when the run started (a copy, so a reconnect during the run changes nothing).</param>
/// <param name="Value">What it carries.</param>
public readonly record struct BlowRunInput(BlowEdge Edge, object? Value)
{
    /// <summary>The node it came from.</summary>
    public string SourceId => Edge.Source;

    /// <summary>The target handle it arrives at, or null for the unnamed one.</summary>
    public string? TargetHandle => Edge.TargetHandle;
}
