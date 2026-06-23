namespace Bit.BlazorUI;

/// <summary>An ATX (<c># Heading</c>) or setext heading. <see cref="Level"/> is 1-6.</summary>
public sealed class BitMarkdownViewerHeadingNode : BitMarkdownViewerMarkdownNode
{
    public int Level { get; init; }
    /// <summary>Optional element id (e.g. set by the auto-identifier extension).</summary>
    public string? Id { get; set; }
    public List<BitMarkdownViewerMarkdownNode> Inlines { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Inlines;
}
