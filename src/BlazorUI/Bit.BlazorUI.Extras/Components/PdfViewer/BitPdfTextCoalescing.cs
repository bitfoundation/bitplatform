namespace Bit.BlazorUI;

/// <summary>How painted text runs are emitted into the page HTML.</summary>
public enum BitPdfTextCoalescing
{
    /// <summary>
    /// Every show-text run keeps its own positioned span, so each glyph run lands
    /// at its exact PDF-computed position. Highest fidelity, but per-glyph PDFs
    /// (one run per character) emit one span per character.
    /// </summary>
    Exact,

    /// <summary>
    /// Adjacent runs on the same baseline with identical style are merged into one
    /// span per visual line, width-corrected to the line's total PDF advance.
    /// Dramatically fewer DOM nodes on per-glyph PDFs, at the cost of small
    /// intra-line position drift (glyphs inside the line are laid out with the
    /// font's own advance widths, so explicit kerning adjustments between runs are
    /// approximated). Rotated text is never coalesced and stays exact.
    /// </summary>
    Compact,
}
