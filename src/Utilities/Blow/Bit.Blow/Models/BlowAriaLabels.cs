namespace Bit.Blow;

/// <summary>
/// Every string the flow reads out or labels itself with, in one place so an app can localize
/// the canvas without reimplementing its accessibility. Assign a new instance to
/// <c>BlowCanvas.AriaLabels</c>; the defaults are English.
/// </summary>
/// <remarks>
/// The <c>Format</c> members are called rather than string-formatted, so a translation is free
/// to reorder or drop the values it is given - something a positional format string cannot do
/// in languages whose word order differs.
/// </remarks>
public sealed class BlowAriaLabels
{
    /// <summary>The English defaults used when no other set is supplied.</summary>
    public static readonly BlowAriaLabels Default = new();

    /// <summary>Accessible name of the node element. Receives the node's own label/id fallback.</summary>
    public Func<string, string> NodeLabel { get; init; } = name => name;

    /// <summary>
    /// What a screen reader calls the kind of thing a node is. Without it a node is announced as
    /// a bare "group", which says nothing about being part of a diagram.
    /// </summary>
    public string NodeRoleDescription { get; init; } = "node";

    /// <summary>What a screen reader calls the kind of thing an edge is.</summary>
    public string EdgeRoleDescription { get; init; } = "edge";

    /// <summary>
    /// Accessible name of an edge that carries no label of its own. Receives the names of the source and target
    /// nodes - each node's aria label, label or id, as a node is announced.
    /// </summary>
    public Func<string, string, string> EdgeLabel { get; init; }
        = (source, target) => $"Edge from {source} to {target}";

    /// <summary>
    /// The state of a node, read after its name as its description: selected or not, its connections, and what a
    /// validation found wrong with it (<c>BlowCanvas.Validation</c>). An empty result leaves only the keyboard
    /// instructions.
    /// </summary>
    public Func<BlowNodeState, string> NodeState { get; init; } = DefaultNodeState;

    /// <summary>The state of an edge, read after its name as its description; see <see cref="NodeState"/>.</summary>
    public Func<BlowEdgeState, string> EdgeState { get; init; } = DefaultEdgeState;

    private static string DefaultNodeState(BlowNodeState state)
    {
        var parts = new List<string>();
        if (state.Node.Selected) parts.Add("Selected");
        if (state.Node.Collapsed) parts.Add("Collapsed");
        if (DefaultRunState(state.Run) is { } run) parts.Add(run);
        AddIssues(parts, state.Issues);
        parts.Add((state.Incoming, state.Outgoing) switch
        {
            (0, 0) => "Not connected",
            (var i, 0) => $"{Count(i, "incoming connection")}",
            (0, var o) => $"{Count(o, "outgoing connection")}",
            (var i, var o) => $"{Count(i, "incoming connection")} and {Count(o, "outgoing connection")}",
        });
        return string.Join(". ", parts) + ".";
    }

    private static string? DefaultRunState(BlowRunState run)
    {
        var status = run.Status switch
        {
            BlowRunStatus.Queued => "Queued",
            BlowRunStatus.Running => "Running",
            BlowRunStatus.Succeeded => "Succeeded",
            BlowRunStatus.Failed => "Failed",
            BlowRunStatus.Skipped => "Skipped",
            _ => null,
        };
        if (status is null) return null;
        return string.IsNullOrEmpty(run.Message) ? status : $"{status}: {run.Message.TrimEnd('.')}";
    }

    private static string DefaultEdgeState(BlowEdgeState state)
    {
        var parts = new List<string>();
        if (state.Edge.Selected) parts.Add("Selected");
        AddIssues(parts, state.Issues);
        return parts.Count == 0 ? "" : string.Join(". ", parts) + ".";
    }

    private static void AddIssues(List<string> parts, IReadOnlyList<BlowValidationIssue> issues)
    {
        // A handful is what a listener can take in; the rest are counted.
        const int Spoken = 3;
        for (var i = 0; i < issues.Count && i < Spoken; i++)
        {
            var issue = issues[i];
            var severity = issue.Severity switch
            {
                BlowValidationSeverity.Error => "Error",
                BlowValidationSeverity.Warning => "Warning",
                _ => "Note",
            };
            parts.Add($"{severity}: {issue.Message.TrimEnd('.')}");
        }
        if (issues.Count > Spoken) parts.Add(Count(issues.Count - Spoken, "more problem"));
    }

    private static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    /// <summary>Announced when a node becomes selected.</summary>
    public Func<string, string> NodeSelected { get; init; } = name => $"{name} selected";

    /// <summary>Announced when a node stops being selected.</summary>
    public Func<string, string> NodeDeselected { get; init; } = name => $"{name} deselected";

    /// <summary>Announced when an edge becomes selected.</summary>
    public Func<string, string, string> EdgeSelected { get; init; }
        = (source, target) => $"Edge {source} to {target} selected";

    /// <summary>Announced when an edge stops being selected.</summary>
    public Func<string, string, string> EdgeDeselected { get; init; }
        = (source, target) => $"Edge {source} to {target} deselected";

    /// <summary>Announced when exactly one node was moved with the keyboard.</summary>
    public Func<string, BlowXYPosition, string> NodeMoved { get; init; }
        = (name, position) => $"{name} moved to {position}";

    /// <summary>Announced when several nodes were moved together with the keyboard.</summary>
    public Func<int, string> NodesMoved { get; init; } = count => $"{count} nodes moved";

    /// <summary>Announced when a node is collapsed. Receives its name and how many nodes were folded away into it.</summary>
    public Func<string, int, string> NodeCollapsed { get; init; } = (name, count) => $"{name} collapsed, {Count(count, "node")} hidden";

    /// <summary>Announced when a node is expanded. Receives its name and how many nodes came back into view.</summary>
    public Func<string, int, string> NodeExpanded { get; init; } = (name, count) => $"{name} expanded, {Count(count, "node")} shown";

    /// <summary>Announced after a deletion.</summary>
    public Func<int, int, string> Deleted { get; init; }
        = (nodes, edges) => $"Deleted {nodes} node(s) and {edges} edge(s)";

    /// <summary>Announced when a connection is made. Receives the names of the source and target nodes.</summary>
    public Func<string, string, string> Connected { get; init; }
        = (source, target) => $"Connected {source} to {target}";

    /// <summary>Announced when a connection the user tried to make was refused, with the reason.</summary>
    public Func<BlowConnectionRejection, string> ConnectionRejected { get; init; } = DefaultRejection;

    /// <summary>
    /// Announced when a connection is started from the keyboard (<c>BlowCanvas.ConnectionKey</c>). Receives the
    /// node's name, the port's label (its declared label, else its id; null for an unnamed handle) and the
    /// connection key, upper-cased.
    /// </summary>
    public Func<string, string?, string, string> KeyboardConnectionStarted { get; init; } = (node, port, key) =>
        $"Connecting from {node}{(port is null ? "" : $", {port}")}. Move to another node and press Enter to "
        + $"connect, {key} to choose another port, or Escape to cancel.";

    /// <summary>
    /// Announced while connecting from the keyboard, when focus reaches a node the connection could end on.
    /// Receives the node's name and the port's label, as <see cref="KeyboardConnectionStarted"/> does.
    /// </summary>
    public Func<string, string?, string> KeyboardConnectionTarget { get; init; } = (node, port) =>
        $"Press Enter to connect to {node}{(port is null ? "" : $", {port}")}.";

    /// <summary>Announced when a connection started from the keyboard is abandoned.</summary>
    public string KeyboardConnectionCancelled { get; init; } = "Connection cancelled.";

    /// <summary>
    /// Announced when the connection key is pressed on a node no connection can start from - it takes none, or
    /// every port it has is full.
    /// </summary>
    public Func<string, string> KeyboardConnectionUnavailable { get; init; } =
        node => $"No connection can start from {node}.";

    /// <summary>
    /// Announced when one end of an existing edge is picked up from the keyboard to be moved (the connection key on a
    /// focused edge). Receives the edge's name, the name of the node the moving end is attached to and the connection
    /// key, upper-cased.
    /// </summary>
    public Func<string, string, string, string> KeyboardReconnectionStarted { get; init; } = (edge, end, key) =>
        $"Moving the end of {edge} at {end}. Move to another node and press Enter to attach it there, {key} to move "
        + "its other end instead, or Escape to leave it as it was.";

    /// <summary>Announced when moving an edge's end from the keyboard is abandoned: the edge stays as it was.</summary>
    public string KeyboardReconnectionCancelled { get; init; } = "Reconnection cancelled. The edge is unchanged.";

    /// <summary>Announced when the connection key is pressed on an edge whose ends cannot be moved. Receives the edge's name.</summary>
    public Func<string, string> KeyboardReconnectionUnavailable { get; init; } =
        edge => $"{edge} cannot be reconnected.";

    /// <summary>Announced when an edge is reconnected. Receives the names of its source and target nodes as they now are.</summary>
    public Func<string, string, string> Reconnected { get; init; }
        = (source, target) => $"Reconnected {source} to {target}";

    private static string DefaultRejection(BlowConnectionRejection reason) => reason switch
    {
        BlowConnectionRejection.NotConnectable => "Cannot connect: that node does not take connections",
        BlowConnectionRejection.SameHandleType => "Cannot connect: an output must be joined to an input",
        BlowConnectionRejection.SelfConnection => "Cannot connect a node to itself",
        BlowConnectionRejection.Duplicate => "Cannot connect: those two are already connected",
        BlowConnectionRejection.IncompatibleTypes => "Cannot connect: the two ports carry incompatible types",
        BlowConnectionRejection.SourceFull => "Cannot connect: the output has no free connections",
        BlowConnectionRejection.TargetFull => "Cannot connect: the input has no free connections",
        BlowConnectionRejection.Cycle => "Cannot connect: the connection would create a cycle",
        BlowConnectionRejection.MissingNode => "Cannot connect: the node is no longer there",
        _ => "Cannot connect",
    };

    /// <summary>
    /// How to use the flow from the keyboard, read by a screen reader as the description of the flow itself
    /// (through <c>aria-describedby</c>), after its name. An empty string leaves the flow undescribed.
    /// </summary>
    /// <remarks>
    /// A canvas gives a screen-reader user nothing to discover its keys by: there is no visible menu, and the
    /// shortcuts are the flow's own. Saying once, where focus first lands, what the keys do is what makes the rest
    /// of the keyboard support findable.
    /// </remarks>
    public string PaneInstructions { get; init; }
        = "Press Tab to move between the nodes and edges of the diagram.";

    /// <summary>
    /// The description of a node (through <c>aria-describedby</c>): what can be done with the focused node. An
    /// empty string leaves nodes undescribed.
    /// </summary>
    public string NodeInstructions { get; init; }
        = "Press Enter or Space to select it, Shift with Enter to add it to the selection, the arrow keys to move "
          + "the selected nodes, Delete to remove them and Escape to clear the selection.";

    /// <summary>
    /// Added to <see cref="NodeInstructions"/> while connections can be made from the keyboard. Receives the key, as
    /// <c>BlowCanvas.ConnectionKey</c> names it, upper-cased.
    /// </summary>
    public Func<string, string> KeyboardConnectionInstructions { get; init; }
        = key => $"Press {key} to start a connection from it.";

    /// <summary>
    /// The description of an edge (through <c>aria-describedby</c>): what can be done with the focused edge. An
    /// empty string leaves edges undescribed.
    /// </summary>
    public string EdgeInstructions { get; init; }
        = "Press Enter or Space to select it, Delete to remove it and Escape to clear the selection.";

    /// <summary>
    /// Added to <see cref="EdgeInstructions"/> while edges can be reconnected from the keyboard. Receives the key, as
    /// <c>BlowCanvas.ConnectionKey</c> names it, upper-cased.
    /// </summary>
    public Func<string, string> KeyboardReconnectionInstructions { get; init; }
        = key => $"Press {key} to move its end to another node.";
}
