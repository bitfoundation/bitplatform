namespace Bit.BlazorUI;

/// <summary>Strong emphasis, rendered as <c>&lt;strong&gt;</c>.</summary>
public sealed class BitMarkdownStrongNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
