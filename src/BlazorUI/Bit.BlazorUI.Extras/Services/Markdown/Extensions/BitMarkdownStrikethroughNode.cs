namespace Bit.BlazorUI;

/// <summary>Strikethrough text (GFM), rendered as <c>&lt;del&gt;</c>.</summary>
public sealed class BitMarkdownViewerStrikethroughNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;
}
