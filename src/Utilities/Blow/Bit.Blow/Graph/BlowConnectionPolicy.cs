namespace Bit.Blow;

/// <summary>The rules a connection has to pass. The canvas builds one from its own parameters.</summary>
public sealed class BlowConnectionPolicy
{
    /// <summary>Whether only a source may join a target (<see cref="BlowConnectionMode.Strict"/>, the default) or any handle any other.</summary>
    public BlowConnectionMode Mode { get; init; } = BlowConnectionMode.Strict;

    /// <summary>Whether a node may be joined to itself, through two different handles. Off by default.</summary>
    public bool AllowSelfConnections { get; init; }

    /// <summary>Whether a second edge with the same source, target and handles is refused. On by default.</summary>
    public bool PreventDuplicates { get; init; } = true;

    /// <summary>
    /// Whether a connection that would close a directed cycle is refused - for pipelines, dependency graphs and
    /// anything else that has to stay acyclic. A self-connection is a cycle. Off by default.
    /// </summary>
    public bool PreventCycles { get; init; }

    /// <summary>
    /// Decides whether data of the source port's <see cref="BlowPort.DataType"/> may flow into the target port's.
    /// Null uses <see cref="BlowConnectionRules.AreTypesCompatible"/>.
    /// </summary>
    public Func<string?, string?, bool>? TypeCompatibility { get; init; }

    /// <summary>An application rule over the connection that would be made; false refuses it.</summary>
    public Func<BlowConnection, bool>? IsValidConnection { get; init; }

    /// <summary>
    /// Finds the port standing for a handle, given its node, the handle type and the handle id. Null uses the
    /// node's declared <see cref="BlowNode.Ports"/>.
    /// </summary>
    public Func<BlowNode, BlowHandleType, string?, BlowPort?>? ResolvePort { get; init; }

    /// <summary>The policy with every default: strict, no self-connections, no duplicates, cycles allowed.</summary>
    public static readonly BlowConnectionPolicy Default = new();
}
