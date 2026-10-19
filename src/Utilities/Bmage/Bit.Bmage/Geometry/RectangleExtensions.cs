using System.Drawing;

namespace Bit.Bmage.Geometry;

/// <summary>
/// Area predicates for <see cref="Rectangle"/>.
/// </summary>
/// <remarks>
/// <see cref="Rectangle.IsEmpty"/> is true only for <c>default(Rectangle)</c>: it asks whether all four fields are
/// zero, not whether the rectangle covers any pixels. That distinction bites after
/// <see cref="Rectangle.Intersect(Rectangle, Rectangle)"/>, which returns a zero-width or zero-height rectangle
/// (never <see cref="Rectangle.Empty"/>) whenever two rectangles merely touch along an edge. Clipping a region at
/// x = width, for example, yields <c>{X=width, Y=0, Width=0, Height=h}</c>, which <c>IsEmpty</c> reports as non-empty
/// and which then indexes past the end of a buffer. Code that means "covers no pixels" must use these helpers.
/// </remarks>
internal static class RectangleExtensions
{
    /// <summary>True when the rectangle covers at least one pixel.</summary>
    public static bool HasArea(this Rectangle rect) => rect.Width > 0 && rect.Height > 0;

    /// <summary>True when the rectangle covers no pixels, whether or not it is <see cref="Rectangle.Empty"/>.</summary>
    public static bool IsEmptyArea(this Rectangle rect) => rect.Width <= 0 || rect.Height <= 0;

    /// <summary>
    /// Intersects with <paramref name="bounds"/> and returns <see cref="Rectangle.Empty"/> rather than a degenerate
    /// rectangle when nothing survives, so the result is safe to test with <see cref="Rectangle.IsEmpty"/> too.
    /// </summary>
    public static Rectangle IntersectClamped(this Rectangle rect, Rectangle bounds)
    {
        rect.Intersect(bounds);
        return rect.HasArea() ? rect : Rectangle.Empty;
    }
}
