namespace Bit.BlazorUI;

/// <summary>
/// Parses a block-level construct. Block parsers are tried in ascending
/// <see cref="Order"/>; the first one that matches the current line wins.
/// </summary>
public abstract class BitMarkdownViewerBlockParser
{
    /// <summary>Relative priority. Lower runs first. The paragraph fallback uses a high value.</summary>
    public virtual int Order => 100;

    /// <summary>
    /// Attempts to parse a block starting at <see cref="BitMarkdownViewerBlockProcessor.Line"/>.
    /// On success the parser appends node(s) via <paramref name="output"/>, advances
    /// <see cref="BitMarkdownViewerBlockProcessor.Line"/> past the consumed lines, and returns true.
    /// </summary>
    public abstract bool TryParse(BitMarkdownViewerBlockProcessor state, List<BitMarkdownViewerMarkdownNode> output);

    /// <summary>
    /// True if this parser's construct begins at <paramref name="lineIndex"/> and is
    /// allowed to interrupt an open paragraph. Used to know where a paragraph ends.
    /// </summary>
    public virtual bool CanInterruptParagraph(BitMarkdownViewerBlockProcessor state, int lineIndex) => false;
}
