namespace Bit.Butil;

/// <summary>
/// One text directive - the <c>#:~:text=</c> part of a scroll-to-text URL, which names a phrase on
/// the page instead of an anchor.
/// </summary>
/// <remarks>
/// The full form is <c>prefix-,start,end,-suffix</c>, and only <see cref="Start"/> is required:
/// <list type="bullet">
/// <item><see cref="Start"/> alone matches that exact phrase.</item>
/// <item><see cref="Start"/> plus <see cref="End"/> matches a whole range, from the first occurrence
/// of one to the next occurrence of the other - the way to link to a paragraph without putting it
/// all in the URL.</item>
/// <item><see cref="Prefix"/> and <see cref="Suffix"/> disambiguate without being highlighted
/// themselves: they say what must sit either side of the match.</item>
/// </list>
/// Matching ignores case and collapses whitespace, and only ever matches visible text.
/// </remarks>
public class TextFragmentDirective
{
    /// <summary>Text that must appear immediately before the match, to disambiguate it. Not highlighted.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>The phrase to find. Required - a directive without it is not emitted.</summary>
    public string Start { get; set; } = string.Empty;

    /// <summary>
    /// The phrase that ends the range. When set, everything from <see cref="Start"/> to here is
    /// highlighted.
    /// </summary>
    public string End { get; set; } = string.Empty;

    /// <summary>Text that must appear immediately after the match, to disambiguate it. Not highlighted.</summary>
    public string Suffix { get; set; } = string.Empty;
}
