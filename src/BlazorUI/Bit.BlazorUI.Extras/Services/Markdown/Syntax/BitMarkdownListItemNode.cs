namespace Bit.BlazorUI;

/// <summary>A single list item containing nested blocks.</summary>
public sealed class BitMarkdownListItemNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;

    /// <summary>
    /// The raw (pre-inline) first content of the item, used to reliably detect
    /// task markers before escaped literals are flattened during inline parsing.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// True once the task-list flavor has recognized a <c>[ ]</c> / <c>[x]</c> marker on this item.
    /// The renderer turns it into the <c>task-list-item</c> class, which is what lets a stylesheet
    /// drop the bullet without relying on <c>:has()</c>.
    /// </summary>
    public bool IsTask { get; set; }
}
