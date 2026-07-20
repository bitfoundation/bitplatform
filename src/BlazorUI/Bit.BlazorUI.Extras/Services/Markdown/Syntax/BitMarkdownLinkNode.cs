namespace Bit.BlazorUI;

/// <summary>A hyperlink.</summary>
public sealed class BitMarkdownLinkNode : BitMarkdownNode
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
