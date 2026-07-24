namespace Bit.BlazorUI;

/// <summary>A paragraph of inline content.</summary>
public sealed class BitMarkdownParagraphNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Inlines { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Inlines;
}
