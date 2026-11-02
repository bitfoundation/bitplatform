namespace Bit.Blow;

/// <summary>
/// What a screen reader is told about a node besides its name: whether it is selected, how it is connected, and the
/// problems a validation found in it. Handed to <see cref="BlowAriaLabels.NodeState"/>, which turns it into the
/// node's accessible description.
/// </summary>
/// <param name="Node">The node.</param>
/// <param name="Incoming">How many edges end at the node.</param>
/// <param name="Outgoing">How many edges start at the node.</param>
/// <param name="Issues">
/// The issues <c>BlowCanvas.Validation</c> lists for the node, its ports included, most serious first.
/// </param>
/// <param name="Run">The node's state in a run, from <c>BlowCanvas.RunStates</c>; <see cref="BlowRunStatus.Idle"/> when none.</param>
public readonly record struct BlowNodeState(
    BlowNode Node,
    int Incoming,
    int Outgoing,
    IReadOnlyList<BlowValidationIssue> Issues,
    BlowRunState Run = default);

/// <summary>
/// What a screen reader is told about an edge besides its name; see <see cref="BlowNodeState"/>.
/// </summary>
/// <param name="Edge">The edge.</param>
/// <param name="Issues">The issues <c>BlowCanvas.Validation</c> lists for the edge, most serious first.</param>
public readonly record struct BlowEdgeState(
    BlowEdge Edge,
    IReadOnlyList<BlowValidationIssue> Issues);
