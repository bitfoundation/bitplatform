using Bit.Bmage;

namespace Bit.Bmage.Tests;

/// <summary>Deterministic synthetic images used across tests.</summary>
public static class TestImages
{
    /// <summary>A smooth gradient with a few sharp features; opaque.</summary>
    public static ImageBuffer Gradient(int width = 64, int height = 48)
    {
        var img = ImageBuffer.Create(width, height);
        for (var y = 0; y < height; y++)
        {
            var row = img.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var r = (byte)(x * 255 / Math.Max(1, width - 1));
                var g = (byte)(y * 255 / Math.Max(1, height - 1));
                var b = (byte)((x + y) * 255 / Math.Max(1, width + height - 2));
                row[x] = new Rgba32(r, g, b);
            }
        }
        return img;
    }

    /// <summary>Four solid quadrants: red (TL), green (TR), blue (BL), white (BR).</summary>
    public static ImageBuffer Quadrants(int width = 8, int height = 8)
    {
        var img = ImageBuffer.Create(width, height);
        for (var y = 0; y < height; y++)
        {
            var row = img.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var left = x < width / 2;
                var top = y < height / 2;
                row[x] = top ? (left ? new Rgba32(255, 0, 0) : new Rgba32(0, 255, 0))
                             : (left ? new Rgba32(0, 0, 255) : new Rgba32(255, 255, 255));
            }
        }
        return img;
    }

    /// <summary>An image with varying alpha, including fully transparent pixels.</summary>
    public static ImageBuffer Translucent(int width = 16, int height = 16)
    {
        var img = ImageBuffer.Create(width, height);
        for (var y = 0; y < height; y++)
        {
            var row = img.GetRow(y);
            for (var x = 0; x < width; x++)
                row[x] = new Rgba32((byte)(x * 16), (byte)(y * 16), 128, (byte)(x * y % 256));
        }
        return img;
    }

    /// <summary>Maximum absolute per-channel difference between two images of the same size.</summary>
    public static int MaxChannelDifference(ImageBuffer a, ImageBuffer b, bool includeAlpha = true)
    {
        if (a.Width != b.Width || a.Height != b.Height) throw new ArgumentException("Size mismatch.");
        var max = 0;
        var pa = a.Pixels;
        var pb = b.Pixels;
        for (var i = 0; i < pa.Length; i++)
        {
            max = Math.Max(max, Math.Abs(pa[i].R - pb[i].R));
            max = Math.Max(max, Math.Abs(pa[i].G - pb[i].G));
            max = Math.Max(max, Math.Abs(pa[i].B - pb[i].B));
            if (includeAlpha) max = Math.Max(max, Math.Abs(pa[i].A - pb[i].A));
        }
        return max;
    }

    /// <summary>Mean absolute per-channel difference between two images of the same size.</summary>
    public static double MeanChannelDifference(ImageBuffer a, ImageBuffer b, bool includeAlpha = false)
    {
        if (a.Width != b.Width || a.Height != b.Height) throw new ArgumentException("Size mismatch.");
        double sum = 0;
        var n = 0;
        var pa = a.Pixels;
        var pb = b.Pixels;
        for (var i = 0; i < pa.Length; i++)
        {
            sum += Math.Abs(pa[i].R - pb[i].R) + Math.Abs(pa[i].G - pb[i].G) + Math.Abs(pa[i].B - pb[i].B);
            n += 3;
            if (includeAlpha) { sum += Math.Abs(pa[i].A - pb[i].A); n++; }
        }
        return sum / n;
    }
}
