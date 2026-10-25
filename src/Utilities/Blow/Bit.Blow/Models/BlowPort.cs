namespace Bit.Blow;

/// <summary>
/// A connection point declared on a node's model: what the node accepts and offers, independent of how its
/// template draws it. Declare ports in <see cref="BlowNode.Ports"/> to give a node typed inputs and outputs,
/// connection limits and required connections that the canvas enforces while the user connects, and that
/// <see cref="BlowGraphValidator"/> can check with no UI at all.
/// </summary>
/// <remarks>
/// <para>
/// A port is the model of a handle. A <c>BlowHandle</c> placed in a node template with the same
/// <see cref="Type"/> and <see cref="Id"/> picks up its definition, so templates keep drawing handles where they
/// like while the rules live with the data. The built-in node types draw one handle per declared port.
/// </para>
/// <para>
/// Declaring ports is optional and per node. A node without any keeps working exactly as before: its handles
/// accept whatever the canvas-wide rules allow.
/// </para>
/// </remarks>
public sealed class BlowPort
{
    /// <summary>
    /// The handle id the port stands for, matched against <c>BlowEdge.SourceHandle</c> or
    /// <c>BlowEdge.TargetHandle</c>. Null for a node's single, unnamed handle of its type.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>Whether edges leave from this port (<see cref="BlowHandleType.Source"/>) or arrive at it.</summary>
    public BlowHandleType Type { get; set; } = BlowHandleType.Source;

    /// <summary>
    /// The side the built-in node types draw the port's handle on. When null they use the node's
    /// <c>SourcePosition</c> or <c>TargetPosition</c>, by type. Ports sharing a side are spread evenly along it.
    /// </summary>
    public BlowPosition? Position { get; set; }

    /// <summary>A short name for the port, used as its handle's tooltip and accessible name.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// The kind of value the port carries - <c>"number"</c>, <c>"image"</c>, <c>"order"</c> - for connections
    /// to be refused between incompatible ports. Null, empty or <c>"*"</c> means any. Several types may be listed,
    /// separated by commas: <c>"int,float"</c> accepts either. Comparison ignores case. The canvas's
    /// <c>ConnectionTypeCompatibility</c> replaces this rule when set.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// The most edges the port may have, or null for no limit. A port at its limit refuses further
    /// connections and cannot start new ones.
    /// </summary>
    public int? MaxConnections { get; set; }

    /// <summary>
    /// The fewest edges the port needs for the graph to be complete. Zero (the default) makes the port optional.
    /// Not enforced while editing - a graph is built one edge at a time - but reported by
    /// <see cref="BlowGraphValidator"/>.
    /// </summary>
    public int MinConnections { get; set; }

    /// <summary>Any payload of yours: a default value, a schema, a description for a properties panel.</summary>
    public object? Data { get; set; }

    /// <summary>A copy of the port. <see cref="Data"/> is shared, as it is when a node is cloned.</summary>
    public BlowPort Clone() => new()
    {
        Id = Id,
        Type = Type,
        Position = Position,
        Label = Label,
        DataType = DataType,
        MaxConnections = MaxConnections,
        MinConnections = MinConnections,
        Data = Data,
    };

    /// <summary>True when the two ports declare the same thing, so a history can tell an edit from a no-op.</summary>
    internal static bool SameValues(BlowPort a, BlowPort b)
        => a.Id == b.Id
           && a.Type == b.Type
           && a.Position == b.Position
           && a.Label == b.Label
           && a.DataType == b.DataType
           && a.MaxConnections == b.MaxConnections
           && a.MinConnections == b.MinConnections
           && Equals(a.Data, b.Data);

    /// <summary>
    /// True when this port stands for the handle of the given type and id. A missing handle id and an empty one
    /// name the same, unnamed handle, as they do everywhere else in the library.
    /// </summary>
    public bool Matches(BlowHandleType type, string? handleId)
        => Type == type && (string.IsNullOrEmpty(Id) ? string.IsNullOrEmpty(handleId) : Id == handleId);
}
