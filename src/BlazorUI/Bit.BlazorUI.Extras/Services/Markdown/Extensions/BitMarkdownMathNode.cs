namespace Bit.BlazorUI;

/// <summary>
/// A run of mathematics, written <c>$inline$</c> or <c>$$display$$</c>. The component does not
/// typeset it - that is what KaTeX and MathJax are for - it keeps the TeX exactly as written,
/// safe from Markdown's own emphasis and escape rules, and marks it so a typesetter can find it.
/// </summary>
public sealed class BitMarkdownMathNode : BitMarkdownNode
{
    /// <summary>The TeX between the delimiters, exactly as the author wrote it.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// True for <c>$$...$$</c>, which a typesetter sets in display style - centred and on a line of
    /// its own - rather than in the run of text.
    /// </summary>
    public bool Display { get; init; }

    /// <summary>
    /// True when the run stood alone as a block, rather than inside a paragraph. Only a block one
    /// is rendered as a <c>div</c>: a <c>div</c> inside a <c>p</c> is markup no browser keeps as
    /// written, and the DOM it rearranges it into is not the one Blazor thinks it rendered.
    /// </summary>
    public bool Block { get; init; }
}
