using System.Numerics;

namespace Bit.Bmage;

/// <summary>A glyph's outline in font units, with the Y axis pointing up as in the font format.</summary>
public sealed class BmageGlyphOutline
{
    public BmageGlyphOutline(IReadOnlyList<BmageGlyphContour> contours) => Contours = contours;
    public IReadOnlyList<BmageGlyphContour> Contours { get; }

    /// <summary>
    /// Appends the glyph to a path builder, converting quadratic segments and flipping the Y axis so the result is in
    /// screen coordinates.
    /// </summary>
    public void AppendTo(BmagePathBuilder builder, Vector2 origin, float scale)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var contour in Contours)
        {
            var pts = contour.Points;
            var n = pts.Count;
            if (n == 0) continue;

            Vector2 Map(BmageGlyphPoint p) => new(origin.X + p.X * scale, origin.Y - p.Y * scale);

            // Find a starting on-curve point; if there is none, synthesise one between two off-curve points.
            var startIndex = -1;
            for (var i = 0; i < n; i++) if (pts[i].OnCurve) { startIndex = i; break; }
            Vector2 startPoint;
            if (startIndex < 0)
            {
                startPoint = (Map(pts[0]) + Map(pts[n - 1])) / 2;
                // The synthesised start sits between the last and first points, so the walk begins at the last one
                // and its first step is to point 0.
                startIndex = n - 1;
                builder.MoveTo(startPoint);
                EmitFrom(builder, pts, n, startIndex, Map, startPoint, allOffCurve: true);
            }
            else
            {
                startPoint = Map(pts[startIndex]);
                builder.MoveTo(startPoint);
                EmitFrom(builder, pts, n, startIndex, Map, startPoint, allOffCurve: false);
            }
            builder.Close();
        }
    }

    private static void EmitFrom(BmagePathBuilder builder, IReadOnlyList<BmageGlyphPoint> pts, int n, int startIndex, Func<BmageGlyphPoint, Vector2> map, Vector2 startPoint, bool allOffCurve)
    {
        Vector2? pendingControl = null;
        var count = allOffCurve ? n : n - 1;
        for (var k = 1; k <= count; k++)
        {
            var p = pts[(startIndex + k) % n];
            var v = map(p);
            if (p.OnCurve)
            {
                if (pendingControl is { } c) { builder.QuadraticTo(c, v); pendingControl = null; }
                else builder.LineTo(v);
            }
            else
            {
                if (pendingControl is { } c)
                {
                    // Two consecutive off-curve points imply an on-curve point halfway between them.
                    var implied = (c + v) / 2;
                    builder.QuadraticTo(c, implied);
                }
                pendingControl = v;
            }
        }
        if (pendingControl is { } last) builder.QuadraticTo(last, startPoint);
    }
}
