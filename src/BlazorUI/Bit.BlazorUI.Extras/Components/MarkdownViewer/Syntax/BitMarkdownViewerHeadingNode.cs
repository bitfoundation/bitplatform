namespace Bit.BlazorUI;

/// <summary>An ATX (<c># Heading</c>) or setext heading. <see cref="Level"/> is 1-6.</summary>
public sealed class BitMarkdownViewerHeadingNode : BitMarkdownViewerMarkdownNode
{
    private readonly int _level = 1;

    public int Level
    {
        get => _level;
        init => _level = value is >= 1 and <= 6
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Heading level must be between 1 and 6.");
    }
    /// <summary>Optional element id (e.g. set by the auto-identifier extension).</summary>
    public string? Id { get; set; }
    public List<BitMarkdownViewerMarkdownNode> Inlines { get; } = new();
    public override IList<BitMarkdownViewerMarkdownNode> ChildNodes => Inlines;
}
