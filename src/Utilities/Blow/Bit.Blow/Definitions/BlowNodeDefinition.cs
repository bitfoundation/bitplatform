namespace Bit.Blow;

/// <summary>
/// What a kind of node is, apart from any one node of it: its name and place in a catalog, the ports it has, the size
/// and data it starts with, how it is checked, and how it runs. Register definitions in a <see cref="BlowNodeRegistry"/>
/// and create nodes from them; the nodes stay plain <see cref="BlowNode"/>s, tied to their definition by
/// <see cref="BlowNode.Type"/> alone.
/// </summary>
public sealed class BlowNodeDefinition
{
    /// <summary>The <see cref="BlowNode.Type"/> of its nodes - also the key of a canvas <c>NodeTypes</c> template.</summary>
    public required string Type { get; init; }

    /// <summary>The name shown in a catalog, and the label new nodes get. The type when null.</summary>
    public string? Label { get; init; }

    /// <summary>The group a catalog lists it under: "Inputs", "Transforms", "AI".</summary>
    public string? Category { get; init; }

    /// <summary>Words a catalog search finds it by, besides its label, type, category and description.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>What it does, for a catalog's tooltip or a properties panel's heading.</summary>
    public string? Description { get; init; }

    /// <summary>An icon for a catalog: a name, a glyph or a URL - whatever the application's catalog draws. Never interpreted by the library.</summary>
    public string? Icon { get; init; }

    /// <summary>The definition's version, for an application keeping track of how its node kinds evolve.</summary>
    public int Version { get; init; } = 1;

    /// <summary>The ports every node of the kind has. Copied to each node created, so a node's ports can change on their own.</summary>
    public IReadOnlyList<BlowPort> Ports { get; init; } = [];

    /// <summary>
    /// Ports that depend on the node's own settings - one input per item of a list in its data, an output per case of a
    /// switch. Called when a node is created and by <see cref="BlowNodeRegistry.RefreshPorts"/>; added after
    /// <see cref="Ports"/>.
    /// </summary>
    public Func<BlowNode, IEnumerable<BlowPort>>? DynamicPorts { get; init; }

    /// <summary>The width new nodes get, or null to size them by their content.</summary>
    public double? Width { get; init; }

    /// <summary>The height new nodes get, or null to size them by their content.</summary>
    public double? Height { get; init; }

    /// <summary>The side new nodes' source handles go on, or null for the node default.</summary>
    public BlowPosition? SourcePosition { get; init; }

    /// <summary>The side new nodes' target handles go on, or null for the node default.</summary>
    public BlowPosition? TargetPosition { get; init; }

    /// <summary>The data a new node starts with - a fresh object each time, so nodes never share their settings.</summary>
    public Func<object?>? CreateData { get; init; }

    /// <summary>Anything else to set on a new node: a class, a style, flags.</summary>
    public Action<BlowNode>? Configure { get; init; }

    /// <summary>
    /// Checks a node of the kind - a missing setting, a value out of range - as part of
    /// <see cref="BlowNodeRegistry.ValidationRule"/>. Issues without a node id get the node's.
    /// </summary>
    public Func<BlowNode, IEnumerable<BlowValidationIssue>>? Validate { get; init; }

    /// <summary>Runs a node of the kind, for a <see cref="BlowRunner"/> given the registry (<see cref="BlowRunner.Use"/>).</summary>
    public BlowNodeHandler? Run { get; init; }

    /// <summary>How nodes of the kind run: timeout, retries, join.</summary>
    public BlowNodeRunOptions? RunOptions { get; init; }

    /// <summary>
    /// Why the kind should no longer be used, when it should not: kept out of catalog searches, reported as a warning by
    /// validation, and turned into <see cref="ReplacedBy"/> by <see cref="BlowNodeRegistry.Upgrade"/>.
    /// </summary>
    public string? Deprecated { get; init; }

    /// <summary>The type that takes the place of a deprecated one.</summary>
    public string? ReplacedBy { get; init; }

    /// <summary>
    /// Turns a node of this deprecated kind into one of <see cref="ReplacedBy"/> - moving settings, renaming ports.
    /// Called by <see cref="BlowNodeRegistry.Upgrade"/> after the type is changed; edges to ports it renames are the
    /// application's to rewire (see <see cref="BlowNodeRegistry.Upgrade"/>).
    /// </summary>
    public Action<BlowNode>? Migrate { get; init; }

    /// <summary>When true, the kind exists for nodes already made but a catalog does not offer it.</summary>
    public bool Hidden { get; init; }

    /// <summary>Anything of the application's: a schema for a properties form, a colour, documentation links.</summary>
    public object? Metadata { get; init; }

    /// <summary>The name to show: <see cref="Label"/>, or the type.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? Type : Label!;
}
