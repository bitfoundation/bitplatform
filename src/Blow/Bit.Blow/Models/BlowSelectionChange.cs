namespace Bit.Blow;

/// <summary>
/// Payload describing the current selection, raised by
/// <c>BlowCanvas.OnSelectionChange</c> whenever the selection changes.
/// </summary>
public readonly record struct BlowSelectionChange(
    IReadOnlyList<BlowNode> Nodes,
    IReadOnlyList<BlowEdge> Edges);
