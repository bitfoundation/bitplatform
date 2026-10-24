using System.Drawing;

namespace Bit.Bmage.Analysis;

/// <summary>Reads the colour of an image at a point: the eyedropper.</summary>
public static class ColorSampler
{
    /// <summary>
    /// The colour at <paramref name="point"/>, averaged over the square of pixels <paramref name="radius"/> around it so
    /// a noisy photo gives a representative colour rather than one grain of its noise. The average is weighted by alpha,
    /// so transparent pixels count for nothing; a point outside the image samples its nearest edge.
    /// </summary>
    /// <param name="image">The image to read.</param>
    /// <param name="point">The pixel to sample.</param>
    /// <param name="radius">0 for the single pixel, 1 for 3x3, 2 for 5x5 and so on.</param>
    public static Rgba32 Sample(ImageBuffer image, Point point, int radius = 0)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        var x = Math.Clamp(point.X, 0, image.Width - 1);
        var y = Math.Clamp(point.Y, 0, image.Height - 1);
        if (radius == 0) return image[x, y];
        var area = Rectangle.FromLTRB(x - radius, y - radius, x + radius + 1, y + radius + 1);
        area.Intersect(image.Bounds);
        double r = 0, g = 0, b = 0, a = 0;
        for (var row = area.Top; row < area.Bottom; row++)
            foreach (var p in image.GetRow(row).Slice(area.Left, area.Width))
            {
                r += p.R * p.A;
                g += p.G * p.A;
                b += p.B * p.A;
                a += p.A;
            }
        if (a <= 0) return Rgba32.Transparent;
        var count = area.Width * area.Height;
        return new Rgba32((byte)Math.Round(r / a), (byte)Math.Round(g / a), (byte)Math.Round(b / a), (byte)Math.Round(a / count));
    }
}
