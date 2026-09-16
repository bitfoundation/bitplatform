namespace Bit.BlazorUI;

/// <summary>
/// The permalink the auto-identifier flavor can append to a heading, so a reader can copy a link
/// straight to that section. It renders as an <c>&lt;a&gt;</c> pointing at the heading's own id.
/// </summary>
public sealed class BitMarkdownHeadingAnchorNode : BitMarkdownNode
{
    /// <summary>The id of the heading this anchor links to.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The heading's plain text, used to give the link an accessible name.</summary>
    public string HeadingText { get; init; } = string.Empty;
}
