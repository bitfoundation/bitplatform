namespace Bit.Blow;

/// <summary>
/// What a screen reader is told about an edge besides its name; see <see cref="BlowNodeState"/>.
/// </summary>
/// <param name="Edge">The edge.</param>
/// <param name="Issues">The issues <c>BlowCanvas.Validation</c> lists for the edge, most serious first.</param>
public readonly record struct BlowEdgeState(
    BlowEdge Edge,
    IReadOnlyList<BlowValidationIssue> Issues);
