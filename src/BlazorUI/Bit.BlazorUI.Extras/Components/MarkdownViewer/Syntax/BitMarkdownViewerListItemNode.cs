namespace Bit.BlazorUI;

/// <summary>A single list item containing nested blocks.</summary>
public sealed class BitMarkdownViewerListItemNode : BitMarkdownViewerMarkdownNode
{
    public List<BitMarkdownViewerMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Children;

    /// <summary>
    /// The raw (pre-inline) first content of the item, used to reliably detect
    /// task markers before escaped literals are flattened during inline parsing.
    /// </summary>
    public string? Source { get; set; }
}
