namespace Bit.BlazorUI;

/// <summary>Emphasis, rendered as <c>&lt;em&gt;</c>.</summary>
public sealed class BitMarkdownEmphasisNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
