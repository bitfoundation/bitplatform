namespace Bit.BlazorUI;

/// <summary>
/// Parses a block-level construct. Block parsers are tried in ascending
/// <see cref="Order"/>; the first one that matches the current line wins.
/// </summary>
public abstract class BitMarkdownBlockParser
{
    /// <summary>Relative priority. Lower runs first. The paragraph fallback uses a high value.</summary>
    public virtual int Order => 100;

    /// <summary>
    /// Attempts to parse a block starting at <see cref="BitMarkdownBlockProcessor.Line"/>.
    /// On success the parser appends node(s) via <paramref name="output"/>, advances
    /// <see cref="BitMarkdownBlockProcessor.Line"/> past the consumed lines, and returns true.
    /// </summary>
    public abstract bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output);

    /// <summary>
    /// True if this parser's construct begins at <paramref name="lineIndex"/> and is
    /// allowed to interrupt an open paragraph. Used to know where a paragraph ends.
    /// </summary>
    public virtual bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex) => false;
}
