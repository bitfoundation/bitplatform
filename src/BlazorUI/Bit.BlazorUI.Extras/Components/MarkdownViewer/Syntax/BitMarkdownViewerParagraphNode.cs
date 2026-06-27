namespace Bit.BlazorUI;

/// <summary>A paragraph of inline content.</summary>
public sealed class BitMarkdownViewerParagraphNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Inlines { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Inlines;
}
