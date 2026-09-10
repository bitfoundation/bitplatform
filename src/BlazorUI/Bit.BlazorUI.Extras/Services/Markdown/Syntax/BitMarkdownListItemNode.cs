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
    /// The document line the item's marker was written on, counted from 0, or <c>-1</c> when the
    /// item cannot be traced back to the source. It is what lets an edit be written back into the
    /// line it came from - a ticked task box into its own <c>[ ]</c> marker - rather than found
    /// again by a second scanner that would have to agree with the parser.
    /// </summary>
    public int SourceLine { get; set; } = -1;

    /// <summary>
    /// True once the task-list flavor has recognized a <c>[ ]</c> / <c>[x]</c> marker on this item.
    /// The renderer turns it into the <c>task-list-item</c> class, which is what lets a stylesheet
    /// drop the bullet without relying on <c>:has()</c>.
    /// </summary>
    public bool IsTask { get; set; }
}
