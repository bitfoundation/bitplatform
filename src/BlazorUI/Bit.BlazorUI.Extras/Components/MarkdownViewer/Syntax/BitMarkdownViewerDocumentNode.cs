namespace Bit.BlazorUI;

/// <summary>Root of a parsed document.</summary>
public sealed class BitMarkdownViewerDocumentNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;
}
