namespace Bit.BlazorUI;

/// <summary>A hyperlink.</summary>
public sealed class BitMarkdownLinkNode : BitMarkdownNode
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }

    /// <summary>
    /// True when the link was not written as one: a bare URL in the text, or an angle-bracket
    /// autolink. Its text is its destination, so a flavor that rewrites text - typographic
    /// replacement, above all - has to leave this one's alone, or the URL a reader copies off the
    /// page stops being the URL the link goes to.
    /// </summary>
    public bool IsAutoLink { get; init; }

    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
