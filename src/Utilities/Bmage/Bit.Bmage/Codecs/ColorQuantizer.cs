namespace Bit.Bmage.Codecs;

/// <summary>An image reduced to a palette: one index per pixel into at most 256 colours.</summary>
/// <param name="Palette">The colours, translucent ones first, then opaque ones.</param>
/// <param name="Indices">One palette index per pixel, row by row.</param>
/// <param name="IsExact">True when the image had no more colours than allowed, so every pixel kept its colour.</param>
public sealed record QuantizedImage(Rgba32[] Palette, byte[] Indices, bool IsExact)
{
    /// <summary>How many palette entries have any transparency; they come first.</summary>
    public int TranslucentCount
    {
        get
        {
            var count = 0;
            while (count < Palette.Length && Palette[count].A < 255) count++;
            return count;
        }
    }
}

/// <summary>
/// Reduces an image to a palette of at most 256 colours, for indexed PNG and GIF: an image with that few colours keeps
/// them all exactly; a photo gets a palette chosen by median cut and, optionally, Floyd-Steinberg dithering.
/// </summary>
/// <remarks>
/// <para>
/// Every fully transparent pixel shares one entry, whatever colour it carries. Other colours are counted at five bits
/// a channel and three bits of alpha, the counted cells split along their widest channel at the median, by pixel count,
/// until there are as many boxes as colours allowed, and each box becomes the mean of the pixels in it.
/// </para>
/// <para>
/// The extra memory is a byte per pixel for the indices and, with dithering, two rows of error terms. An image that
/// needs a palette chosen (more colours than allowed) also takes about 9.5 MB of tables for each call: a count and
/// four channel sums for each of the 2^18 cells (1 MB and 8 MB) and a nearest-entry cache of the same cells (0.5 MB).
/// An image with few enough colours to keep them all needs none of these.
/// </para>
/// </remarks>
internal static class ColorQuantizer
{
    /// <summary>
    /// Quantizes <paramref name="image"/> to at most <paramref name="maxColors"/> colours (2 to 256).
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="maxColors">The most colours the palette may hold, transparency counting as one.</param>
    /// <param name="dither">Diffuse each pixel's error to its neighbours, which hides banding in gradients at the cost of grain.</param>
    /// <param name="alphaLevels">
    /// How many levels of transparency to keep: 256 for PNG, which gives every palette entry its own alpha, or 2 for GIF,
    /// whose pixels are either opaque or transparent. With 2, a pixel under half opaque becomes transparent.
    /// </param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public static QuantizedImage Quantize(ImageBuffer image, int maxColors, bool dither, int alphaLevels = 256, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (maxColors is < 2 or > 256) throw new ArgumentOutOfRangeException(nameof(maxColors), maxColors, "The palette holds between 2 and 256 colours.");
        var binaryAlpha = alphaLevels == 2;

        if (TryExact(image, maxColors, binaryAlpha, cancellationToken) is { } exact) return exact;

        // Histogram of 5-5-5 bit colour and 3 bit alpha cells; transparent pixels kept apart.
        const int cells = 1 << 18;
        var counts = new int[cells];
        var sums = new long[cells * 4];
        var transparent = false;
        for (var y = 0; y < image.Height; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            foreach (var raw in image.GetRow(y))
            {
                var p = binaryAlpha ? Binarize(raw) : raw;
                if (p.A == 0) { transparent = true; continue; }
                var key = Key(p);
                counts[key]++;
                sums[key * 4] += p.R;
                sums[key * 4 + 1] += p.G;
                sums[key * 4 + 2] += p.B;
                sums[key * 4 + 3] += p.A;
            }
        }

        var opaqueSlots = maxColors - (transparent ? 1 : 0);
        var palette = MedianCut(counts, sums, Math.Max(1, opaqueSlots));
        if (transparent) palette.Insert(0, Rgba32.Transparent);
        var ordered = Order(palette);
        var indices = Map(image, ordered, dither, binaryAlpha, cancellationToken);
        return new QuantizedImage(ordered, indices, IsExact: false);
    }

    /// <summary>The exact palette when the image has no more distinct colours than allowed, else null.</summary>
    private static QuantizedImage? TryExact(ImageBuffer image, int maxColors, bool binaryAlpha, CancellationToken cancellationToken)
    {
        var colours = new Dictionary<uint, int>(maxColors + 1);
        for (var y = 0; y < image.Height; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            foreach (var raw in image.GetRow(y))
            {
                var p = Normalize(binaryAlpha ? Binarize(raw) : raw);
                if (colours.TryAdd(p.PackedValue, 0) && colours.Count > maxColors) return null;
            }
        }
        var palette = Order(colours.Keys.Select(Rgba32.FromPacked).ToList());
        var lookup = new Dictionary<uint, byte>(palette.Length);
        for (var i = 0; i < palette.Length; i++) lookup[palette[i].PackedValue] = (byte)i;
        var indices = new byte[image.PixelCount];
        for (var y = 0; y < image.Height; y++)
        {
            var row = image.GetRow(y);
            var line = indices.AsSpan(y * image.Width, image.Width);
            for (var x = 0; x < row.Length; x++) line[x] = lookup[Normalize(binaryAlpha ? Binarize(row[x]) : row[x]).PackedValue];
        }
        return new QuantizedImage(palette, indices, IsExact: true);
    }

    /// <summary>Every fully transparent pixel is the same colour: transparent black.</summary>
    private static Rgba32 Normalize(Rgba32 p) => p.A == 0 ? Rgba32.Transparent : p;

    private static Rgba32 Binarize(Rgba32 p) => p.A < 128 ? Rgba32.Transparent : p.WithAlpha(255);

    private static int Key(Rgba32 p) => (p.R >> 3) << 13 | (p.G >> 3) << 8 | (p.B >> 3) << 3 | p.A >> 5;

    /// <summary>Translucent entries first, so PNG's tRNS chunk lists only them; otherwise as found.</summary>
    private static Rgba32[] Order(List<Rgba32> palette) => [.. palette.Where(c => c.A < 255), .. palette.Where(c => c.A == 255)];

    private sealed class Box
    {
        public required int[] Keys;
        public long Count;
        public int Channel;
        public long Score;
    }

    private static List<Rgba32> MedianCut(int[] counts, long[] sums, int colours)
    {
        var keys = new List<int>();
        for (var k = 0; k < counts.Length; k++) if (counts[k] > 0) keys.Add(k);
        var boxes = new List<Box> { Measure([.. keys], counts) };
        while (boxes.Count < colours)
        {
            // Split the box whose widest channel, weighted by the pixels in it, is largest.
            var index = -1;
            for (var i = 0; i < boxes.Count; i++)
                if (boxes[i].Keys.Length > 1 && (index < 0 || boxes[i].Score > boxes[index].Score)) index = i;
            if (index < 0) break;
            var box = boxes[index];
            var channel = box.Channel;
            var sorted = box.Keys.OrderBy(k => Component(k, channel)).ThenBy(k => k).ToArray();
            // At the pixel-weighted median, keeping at least one cell each side.
            long half = box.Count / 2, seen = 0;
            var cut = 1;
            for (var i = 0; i < sorted.Length - 1; i++)
            {
                seen += counts[sorted[i]];
                cut = i + 1;
                if (seen >= half) break;
            }
            boxes[index] = Measure(sorted[..cut], counts);
            boxes.Add(Measure(sorted[cut..], counts));
        }

        var palette = new List<Rgba32>(boxes.Count);
        foreach (var box in boxes)
        {
            long r = 0, g = 0, b = 0, a = 0, n = 0;
            foreach (var k in box.Keys)
            {
                r += sums[k * 4]; g += sums[k * 4 + 1]; b += sums[k * 4 + 2]; a += sums[k * 4 + 3];
                n += counts[k];
            }
            palette.Add(new Rgba32((byte)((r + n / 2) / n), (byte)((g + n / 2) / n), (byte)((b + n / 2) / n), (byte)((a + n / 2) / n)));
        }
        // Boxes can average to the same colour; one entry each is enough.
        return palette.Distinct().ToList();
    }

    /// <summary>A channel of a cell's key, on a common 0..31 scale (alpha's three bits are spread over it).</summary>
    private static int Component(int key, int channel) => channel switch
    {
        0 => key >> 13 & 31,
        1 => key >> 8 & 31,
        2 => key >> 3 & 31,
        _ => (key & 7) * 31 / 7,
    };

    private static Box Measure(int[] keys, int[] counts)
    {
        Span<int> min = [31, 31, 31, 31];
        Span<int> max = [0, 0, 0, 0];
        long count = 0;
        foreach (var k in keys)
        {
            count += counts[k];
            for (var c = 0; c < 4; c++)
            {
                var v = Component(k, c);
                if (v < min[c]) min[c] = v;
                if (v > max[c]) max[c] = v;
            }
        }
        // Green matters most to the eye and blue least, as in the distance the pixels are mapped with.
        Span<int> weight = [3, 4, 2, 3];
        int best = 0, bestRange = -1;
        for (var c = 0; c < 4; c++)
        {
            var range = (max[c] - min[c]) * weight[c];
            if (range > bestRange) (best, bestRange) = (c, range);
        }
        return new Box { Keys = keys, Count = count, Channel = best, Score = bestRange * count };
    }

    /// <summary>Maps every pixel to its nearest entry, through a cache of the nearest entry for each 5-5-5-3 cell.</summary>
    private static byte[] Map(ImageBuffer image, Rgba32[] palette, bool dither, bool binaryAlpha, CancellationToken cancellationToken)
    {
        var cache = new short[1 << 18];
        Array.Fill(cache, (short)-1);
        var transparentIndex = Array.FindIndex(palette, c => c.A == 0);
        int width = image.Width, height = image.Height;
        var indices = new byte[image.PixelCount];

        // Error terms for this row and the next, three channels, with a pixel of room either side.
        var current = dither ? new float[(width + 2) * 3] : [];
        var next = dither ? new float[(width + 2) * 3] : [];
        for (var y = 0; y < height; y++)
        {
            if ((y & 31) == 0) cancellationToken.ThrowIfCancellationRequested();
            var row = image.GetRow(y);
            var line = indices.AsSpan(y * width, width);
            if (dither) Array.Clear(next);
            // Serpentine: every other row runs right to left, so the error does not drift one way.
            var reverse = dither && (y & 1) == 1;
            for (var i = 0; i < width; i++)
            {
                var x = reverse ? width - 1 - i : i;
                var p = binaryAlpha ? Binarize(row[x]) : row[x];
                if (p.A == 0 && transparentIndex >= 0)
                {
                    line[x] = (byte)transparentIndex;
                    continue;
                }
                if (dither)
                {
                    var e = (x + 1) * 3;
                    p = new Rgba32(Rgba32.ClampToByte(p.R + current[e]), Rgba32.ClampToByte(p.G + current[e + 1]), Rgba32.ClampToByte(p.B + current[e + 2]), p.A);
                }
                var key = Key(p);
                var index = cache[key];
                if (index < 0) cache[key] = index = (short)Nearest(palette, p, opaqueOnly: binaryAlpha);
                line[x] = (byte)index;
                if (!dither) continue;

                var chosen = palette[index];
                float dr = p.R - chosen.R, dg = p.G - chosen.G, db = p.B - chosen.B;
                // Floyd-Steinberg: 7/16 ahead, 3/16 behind below, 5/16 below, 1/16 ahead below.
                var step = reverse ? -1 : 1;
                Spread(current, x + 1 + step, dr, dg, db, 7 / 16f);
                Spread(next, x + 1 - step, dr, dg, db, 3 / 16f);
                Spread(next, x + 1, dr, dg, db, 5 / 16f);
                Spread(next, x + 1 + step, dr, dg, db, 1 / 16f);
            }
            if (dither) (current, next) = (next, current);
        }
        return indices;
    }

    private static void Spread(float[] errors, int column, float dr, float dg, float db, float share)
    {
        var e = column * 3;
        errors[e] += dr * share;
        errors[e + 1] += dg * share;
        errors[e + 2] += db * share;
    }

    /// <summary>
    /// The entry nearest <paramref name="p"/>. With <paramref name="opaqueOnly"/>, as for GIF, whose pixels are opaque or
    /// transparent, the transparent entry is passed over: alpha's weight would otherwise send an opaque colour far from
    /// every opaque entry, such as black against a palette of white, to it.
    /// </summary>
    private static int Nearest(Rgba32[] palette, Rgba32 p, bool opaqueOnly)
    {
        int best = 0;
        var bestDistance = long.MaxValue;
        for (var i = 0; i < palette.Length; i++)
        {
            var c = palette[i];
            if (opaqueOnly && c.A == 0) continue;
            int dr = p.R - c.R, dg = p.G - c.G, db = p.B - c.B, da = p.A - c.A;
            var distance = 3L * dr * dr + 4L * dg * dg + 2L * db * db + 4L * da * da;
            if (distance < bestDistance) (best, bestDistance) = (i, distance);
        }
        return best;
    }
}
