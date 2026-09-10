namespace Bit.BlazorUI;

/// <summary>
/// An image that stands on its own with a caption, rendered as
/// <c>&lt;figure&gt;&lt;img&gt;&lt;figcaption&gt;</c>.
/// </summary>
/// <remarks>
/// The image is held as an ordinary child rather than as a property of its own, so every generic
/// pass over the tree still reaches it - the viewer's image policy included, which would otherwise
/// let a cross-origin image load simply because it had been given a caption.
/// </remarks>
public sealed class BitMarkdownFigureNode : BitMarkdownNode
{
    /// <summary>The figure's content: the image it is built around.</summary>
    public List<BitMarkdownNode> Children { get; } = new();

    public override IList<BitMarkdownNode> ChildNodes => Children;

    /// <summary>The caption, taken from the image's title.</summary>
    public string Caption { get; init; } = string.Empty;
}
