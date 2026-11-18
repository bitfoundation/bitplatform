using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Renders text for the pure .NET rasterizer. The library has no font engine, so applications that need text in
/// server-side PNG export supply one (backed by SkiaSharp, ImageSharp or a bitmap font).
/// </summary>
public interface IBinkTextRasterizer
{
    /// <summary>
    /// Draws text into the surface. Coordinates are in device pixels and the transform has already been applied
    /// to <paramref name="origin"/>; <paramref name="scale"/> is the current transform's scale factor.
    /// </summary>
    void DrawText(BinkRasterSurface surface, string text, Vector2 origin, in BinkTextStyle style, float scale, float opacity, BinkRect clip);

    /// <summary>Measures a single line of text in style units, or null when the implementation cannot measure.</summary>
    Vector2? Measure(string text, in BinkTextStyle style);
}
