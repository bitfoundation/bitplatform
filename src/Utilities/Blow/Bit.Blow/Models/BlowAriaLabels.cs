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

    /// <summary>Accessible name of an edge that carries no label of its own.</summary>
    public Func<string, string, string> EdgeLabel { get; init; }
        = (source, target) => $"Edge from {source} to {target}";

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

    /// <summary>Announced after a deletion.</summary>
    public Func<int, int, string> Deleted { get; init; }
        = (nodes, edges) => $"Deleted {nodes} node(s) and {edges} edge(s)";
}
