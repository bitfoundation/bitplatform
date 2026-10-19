
namespace Bit.BbScanner.TestKit;

/// <summary>
/// Renders symbols into grayscale images so that decoders can be exercised end to end without
/// binary test fixtures.
/// </summary>
public static class SyntheticImage
{
    /// <summary>Renders a module matrix into a grayscale image.</summary>
    /// <param name="matrix">The module grid; a set bit is dark.</param>
    /// <param name="scale">Pixels per module.</param>
    /// <param name="quietZoneModules">Light border width in modules.</param>
    /// <param name="dark">Luminance of a dark module.</param>
    /// <param name="light">Luminance of a light module.</param>
    public static BbLuminanceBuffer FromMatrix(
        BbBitMatrix matrix,
        int scale = 4,
        int quietZoneModules = 4,
        byte dark = 0,
        byte light = 255)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var width = (matrix.Width + (2 * quietZoneModules)) * scale;
        var height = (matrix.Height + (2 * quietZoneModules)) * scale;
        var buffer = BbLuminanceBuffer.Rent(width, height);
        var pixels = buffer.Pixels;
        pixels.Fill(light);

        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                if (!matrix[x, y])
                {
                    continue;
                }

                var px = (x + quietZoneModules) * scale;
                var py = (y + quietZoneModules) * scale;
                for (var dy = 0; dy < scale; dy++)
                {
                    pixels.Slice(((py + dy) * width) + px, scale).Fill(dark);
                }
            }
        }

        return buffer;
    }

    /// <summary>Renders a linear symbol into a grayscale image.</summary>
    /// <param name="modules">One entry per module; <see langword="true"/> is a bar.</param>
    /// <param name="scale">Pixels per module.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="quietZoneModules">Light border width in modules on each side.</param>
    public static BbLuminanceBuffer FromLinear(
        ReadOnlySpan<bool> modules,
        int scale = 3,
        int height = 60,
        int quietZoneModules = 12)
    {
        var width = (modules.Length + (2 * quietZoneModules)) * scale;
        var buffer = BbLuminanceBuffer.Rent(width, height);
        var pixels = buffer.Pixels;
        pixels.Fill(255);

        for (var i = 0; i < modules.Length; i++)
        {
            if (!modules[i])
            {
                continue;
            }

            var px = (i + quietZoneModules) * scale;
            for (var y = 0; y < height; y++)
            {
                pixels.Slice((y * width) + px, scale).Fill(0);
            }
        }

        return buffer;
    }

    /// <summary>Expands run-length element widths into a module array, starting with a bar.</summary>
    /// <param name="widths">Element widths in modules, bar first.</param>
    public static bool[] ExpandWidths(ReadOnlySpan<int> widths)
    {
        var total = 0;
        foreach (var width in widths)
        {
            total += width;
        }

        var modules = new bool[total];
        var index = 0;
        var bar = true;
        foreach (var width in widths)
        {
            for (var i = 0; i < width; i++)
            {
                modules[index++] = bar;
            }

            bar = !bar;
        }

        return modules;
    }

    /// <summary>Adds uniform noise to an image, used to check decoder robustness.</summary>
    /// <param name="buffer">The image to perturb, in place.</param>
    /// <param name="amplitude">Maximum absolute change per pixel.</param>
    /// <param name="seed">Random seed, so failures are reproducible.</param>
    public static void AddNoise(BbLuminanceBuffer buffer, int amplitude, int seed)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var random = new Random(seed);
        var pixels = buffer.Pixels;
        for (var i = 0; i < pixels.Length; i++)
        {
            var value = pixels[i] + random.Next(-amplitude, amplitude + 1);
            pixels[i] = (byte)Math.Clamp(value, 0, 255);
        }
    }
}
