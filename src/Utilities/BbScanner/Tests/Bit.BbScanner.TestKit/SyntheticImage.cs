
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

    /// <summary>
    /// Renders a MaxiCode symbol: its 33 rows of 30 hexagonal modules, every odd row half a module to the right, and the
    /// bullseye's three rings round the place of the module at row 16, column 14. Each pixel is sampled four by four,
    /// so edges fall between pixels the way a camera sees them.
    /// </summary>
    /// <param name="modules">The 30 by 33 grid; <see langword="true"/> is a dark module.</param>
    /// <param name="moduleWidth">The width of a module, in pixels; a row is 0.866 of it below the last.</param>
    /// <param name="quietZoneModules">Light border width in module widths on each side.</param>
    public static BbLuminanceBuffer FromMaxiCode(BbBitMatrix modules, double moduleWidth = 6, int quietZoneModules = 2)
    {
        ArgumentNullException.ThrowIfNull(modules);
        const double Pitch = 0.8660254037844386;
        const double HexRadius = 0.5773502691896258; // centre to corner of a hexagon a module wide
        const double Gap = 0.9; // the hexagons drawn a little smaller than their cells, as printers leave them
        double[] rings = [0.5774, 1.3359, 2.1058, 2.8644, 3.6229, 4.3814];

        var margin = quietZoneModules * moduleWidth;
        var width = (int)Math.Ceiling((30.5 * moduleWidth) + (2 * margin));
        var height = (int)Math.Ceiling(((32 * Pitch) + (2 * HexRadius)) * moduleWidth + (2 * margin));
        var buffer = BbLuminanceBuffer.Rent(width, height);
        var pixels = buffer.Pixels;

        double CentreX(int row, int column) => margin + ((column + 0.5 + ((row & 1) == 1 ? 0.5 : 0)) * moduleWidth);
        double CentreY(int row) => margin + ((HexRadius + (row * Pitch)) * moduleWidth);
        var (bullseyeX, bullseyeY) = (CentreX(16, 14), CentreY(16));

        bool IsDark(double x, double y)
        {
            // The rings first: dark between the first and second circle, the third and fourth, the fifth and sixth.
            var distance = Math.Sqrt(((x - bullseyeX) * (x - bullseyeX)) + ((y - bullseyeY) * (y - bullseyeY))) / moduleWidth;
            for (var ring = 0; ring < 6; ring += 2)
            {
                if (distance >= rings[ring] && distance < rings[ring + 1])
                {
                    return true;
                }
            }

            // Then the hexagon whose row and column the point is nearest.
            var row = (int)Math.Round(((y - margin) / moduleWidth - HexRadius) / Pitch);
            for (var r = row - 1; r <= row + 1; r++)
            {
                if (r < 0 || r >= modules.Height)
                {
                    continue;
                }

                var column = (int)Math.Round(((x - margin) / moduleWidth) - 0.5 - ((r & 1) == 1 ? 0.5 : 0));
                for (var c = column - 1; c <= column + 1; c++)
                {
                    if (c < 0 || c >= modules.Width || !modules[c, r])
                    {
                        continue;
                    }

                    // A hexagon standing on a corner: within half its width across, and below the slanted edges.
                    var dx = Math.Abs(x - CentreX(r, c)) / (moduleWidth * Gap);
                    var dy = Math.Abs(y - CentreY(r)) / (moduleWidth * Gap);
                    if (dx <= 0.5 && dy <= HexRadius - (dx / Math.Sqrt(3)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        for (var py = 0; py < height; py++)
        {
            for (var px = 0; px < width; px++)
            {
                var dark = 0;
                for (var sy = 0; sy < 4; sy++)
                {
                    for (var sx = 0; sx < 4; sx++)
                    {
                        if (IsDark(px + ((sx + 0.5) / 4), py + ((sy + 0.5) / 4)))
                        {
                            dark++;
                        }
                    }
                }

                pixels[(py * width) + px] = (byte)(255 - (dark * 255 / 16));
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

    /// <summary>
    /// Renders a linear symbol with every bar drawn wider or narrower than its modules, as ink spread, a thermal
    /// printer's dot gain or an underexposed photograph make it: every bar's width changes by
    /// <paramref name="barGain"/> pixels, half on each side and the odd pixel on the right, and the spaces give or
    /// take the difference.
    /// </summary>
    /// <param name="modules">One entry per module; <see langword="true"/> is a bar.</param>
    /// <param name="scale">Pixels per module.</param>
    /// <param name="barGain">Pixels added to every bar; negative narrows them.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="quietZoneModules">Light border width in modules on each side.</param>
    public static BbLuminanceBuffer FromLinearWithBarGain(
        ReadOnlySpan<bool> modules,
        int scale,
        int barGain,
        int height = 60,
        int quietZoneModules = 12)
    {
        var width = (modules.Length + (2 * quietZoneModules)) * scale;
        var buffer = BbLuminanceBuffer.Rent(width, height);
        var pixels = buffer.Pixels;
        pixels.Fill(255);

        var left = barGain / 2;
        var right = barGain - left;
        for (var i = 0; i < modules.Length; i++)
        {
            if (!modules[i] || (i > 0 && modules[i - 1]))
            {
                continue;
            }

            var end = i;
            while (end < modules.Length && modules[end])
            {
                end++;
            }

            var from = ((i + quietZoneModules) * scale) - left;
            var to = ((end + quietZoneModules) * scale) + right;
            if (to <= from)
            {
                continue;
            }

            for (var y = 0; y < height; y++)
            {
                pixels.Slice((y * width) + from, to - from).Fill(0);
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
