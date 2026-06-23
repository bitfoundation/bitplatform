namespace Bit.BlazorUI;

/// <summary>A single list item containing nested blocks.</summary>
public sealed class BitMarkdownViewerListItemNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;
}
