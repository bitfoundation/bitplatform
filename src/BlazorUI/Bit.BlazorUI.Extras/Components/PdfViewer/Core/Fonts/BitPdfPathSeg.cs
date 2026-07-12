// Type1 (PostScript) font-program parsing and charstring interpretation.
// Embedded Type1 fonts (/FontFile) can't be loaded by a browser directly, so we
// parse the program, interpret each glyph's Type1 charstring into an absolute
// outline, and hand the outlines to CffFontWriter to build an OpenType/CFF font.
//
// Hints are intentionally dropped: unhinted outlines render with the correct
// shape (only rasterization crispness at tiny sizes is affected), which lets us
// skip the intricate hint-replacement machinery.

namespace Bit.BlazorUI;

/// <summary>One outline contour segment (absolute glyph-space coordinates).</summary>
internal readonly struct BitPdfPathSeg
{
    public enum Kind : byte { Move, Line, Curve, Close }
    public Kind Op { get; init; }
    public double X1 { get; init; }
    public double Y1 { get; init; }
    public double X2 { get; init; }
    public double Y2 { get; init; }
    public double X3 { get; init; }
    public double Y3 { get; init; }
}
