namespace Bit.BlazorUI;

/// <summary>Strong emphasis, rendered as <c>&lt;strong&gt;</c>.</summary>
public sealed class BitMarkdownViewerStrongNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;
}
