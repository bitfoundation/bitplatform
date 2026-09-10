namespace Bit.BlazorUI;

/// <summary>
/// A custom container: a <c>:::</c>-fenced block whose info word names what it is, the way docs
/// sites write admonitions (<c>:::warning</c>) and layout blocks (<c>:::grid</c>).
/// </summary>
public sealed class BitMarkdownContainerNode : BitMarkdownNode
{
    /// <summary>
    /// The container's name, lower-cased and slugified, which becomes part of its class name.
    /// Empty when the fence carried no info string.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The rest of the info string after the name - the container's title, when it has one.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>The blocks fenced by the container.</summary>
    public List<BitMarkdownNode> Children { get; } = new();

    public override IList<BitMarkdownNode> ChildNodes => Children;
}
