namespace Bit.BlazorUI;

/// <summary>Root of a parsed document.</summary>
public sealed class BitMarkdownDocumentNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
