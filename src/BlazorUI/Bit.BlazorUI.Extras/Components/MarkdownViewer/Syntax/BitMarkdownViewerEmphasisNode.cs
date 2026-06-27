namespace Bit.BlazorUI;

/// <summary>Emphasis, rendered as <c>&lt;em&gt;</c>.</summary>
public sealed class BitMarkdownViewerEmphasisNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;
}
