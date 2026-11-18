namespace Bit.Bmage;

/// <summary>One closed contour of a glyph.</summary>
public sealed class BmageGlyphContour
{
    public BmageGlyphContour(IReadOnlyList<BmageGlyphPoint> points) => Points = points;
    public IReadOnlyList<BmageGlyphPoint> Points { get; }
}
