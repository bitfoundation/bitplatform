namespace Bit.BlazorUI;

/// <summary>Strikethrough text (GFM), rendered as <c>&lt;del&gt;</c>.</summary>
public sealed class BitMarkdownStrikethroughNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
