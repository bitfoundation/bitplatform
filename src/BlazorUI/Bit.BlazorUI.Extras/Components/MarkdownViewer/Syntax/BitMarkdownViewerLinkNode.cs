namespace Bit.BlazorUI;

/// <summary>A hyperlink.</summary>
public sealed class BitMarkdownViewerLinkNode : BitMarkdownViewerMarkdownNode
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;
}
