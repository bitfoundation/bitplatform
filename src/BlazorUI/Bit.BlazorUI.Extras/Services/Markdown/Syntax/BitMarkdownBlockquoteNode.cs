namespace Bit.BlazorUI;

/// <summary>A block quote containing nested blocks.</summary>
public sealed class BitMarkdownBlockquoteNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
