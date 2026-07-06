namespace Bit.BlazorUI;

/// <summary>A block quote containing nested blocks.</summary>
public sealed class BitMarkdownViewerBlockquoteNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;
}
