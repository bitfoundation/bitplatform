// Type1 (PostScript) font-program parsing and charstring interpretation.
// Embedded Type1 fonts (/FontFile) can't be loaded by a browser directly, so we
// parse the program, interpret each glyph's Type1 charstring into an absolute
// outline, and hand the outlines to CffFontWriter to build an OpenType/CFF font.
//
// Hints are intentionally dropped: unhinted outlines render with the correct
// shape (only rasterization crispness at tiny sizes is affected), which lets us
// skip the intricate hint-replacement machinery.

namespace Bit.BlazorUI;

/// <summary>A decoded glyph outline plus its advance width.</summary>
internal sealed class BitPdfGlyphOutline
{
    public double AdvanceWidth { get; set; }
    public List<BitPdfPathSeg> Segments { get; } = new();
}
