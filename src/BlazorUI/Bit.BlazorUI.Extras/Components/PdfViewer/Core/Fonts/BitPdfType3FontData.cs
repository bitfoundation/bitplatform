// The font model: the parts needed to position and extract text.


namespace Bit.BlazorUI;

/// <summary>
/// The data needed to draw a Type3 font's glyphs: the font matrix (glyph space
/// to text space), the <c>/CharProcs</c> content streams, the glyph resources,
/// and the code-to-glyph-name encoding. Type3 glyphs are rendered by executing
/// each glyph's content stream, unlike other fonts whose glyphs are drawn from
/// outlines or substituted.
/// </summary>
public sealed class BitPdfType3FontData
{
    private readonly BitPdfDict? _charProcs;
    private readonly string[]? _encoding;
    private readonly IBitPdfXRef _xref;

    /// <summary>The font matrix mapping glyph space to text space.</summary>
    public BitPdfMatrix FontMatrix { get; }

    /// <summary>The glyph resource dictionary, if the font supplies one.</summary>
    public BitPdfDict? Resources { get; }

    internal BitPdfType3FontData(BitPdfMatrix fontMatrix, BitPdfDict? charProcs, BitPdfDict? resources,
        string[]? encoding, IBitPdfXRef xref)
    {
        FontMatrix = fontMatrix;
        _charProcs = charProcs;
        Resources = resources;
        _encoding = encoding;
        _xref = xref;
    }

    /// <summary>
    /// Returns the glyph procedure (a content stream) for a character code, or
    /// <c>null</c> when the code has no glyph name or matching <c>/CharProcs</c> entry.
    /// </summary>
    public BitPdfStream? GetGlyphProcedure(int code)
    {
        if (_charProcs is null || _encoding is null || code is < 0 or > 255)
        {
            return null;
        }
        string name = _encoding[code];
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        return _xref.FetchIfRef(_charProcs.Get(name)) as BitPdfStream;
    }
}
