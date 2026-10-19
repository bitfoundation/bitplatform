using System.Drawing;
using Bit.Bmage.Operations;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Analysis;

/// <summary>
/// A 256-bucket histogram for one channel, with the derived values that adjustments actually need.
/// </summary>
public sealed class Histogram
{
    private readonly int[] _buckets;

    internal Histogram(int[] buckets, long total)
    {
        _buckets = buckets;
        Total = total;
    }

    /// <summary>Counts per level, 0 to 255.</summary>
    public IReadOnlyList<int> Buckets => _buckets;

    /// <summary>Number of pixels counted.</summary>
    public long Total { get; }

    /// <summary>The count at a level.</summary>
    public int this[int level] => _buckets[level];

    /// <summary>The lowest level with any pixels, or 0 when the histogram is empty.</summary>
    public int Minimum
    {
        get
        {
            for (var i = 0; i < 256; i++) if (_buckets[i] > 0) return i;
            return 0;
        }
    }

    /// <summary>The highest level with any pixels, or 255 when the histogram is empty.</summary>
    public int Maximum
    {
        get
        {
            for (var i = 255; i >= 0; i--) if (_buckets[i] > 0) return i;
            return 255;
        }
    }

    /// <summary>The mean level.</summary>
    public double Mean
    {
        get
        {
            if (Total == 0) return 0;
            double sum = 0;
            for (var i = 0; i < 256; i++) sum += (double)_buckets[i] * i;
            return sum / Total;
        }
    }

    /// <summary>The median level.</summary>
    public int Median => GetPercentile(0.5);

    /// <summary>
    /// The level below which <paramref name="fraction"/> of the pixels fall. Used by auto-levels to ignore a few
    /// outlying pixels rather than letting a single hot pixel define the white point.
    /// </summary>
    public int GetPercentile(double fraction)
    {
        if (Total == 0) return 0;
        // At least one pixel: a target of zero is met by an empty first bucket, so the 0th percentile would always be
        // level 0 instead of the darkest level actually present.
        var target = Math.Max(1, (long)Math.Ceiling(Total * Math.Clamp(fraction, 0, 1)));
        long running = 0;
        for (var i = 0; i < 256; i++)
        {
            running += _buckets[i];
            if (running >= target) return i;
        }
        return 255;
    }

    /// <summary>The largest bucket count, useful for scaling a histogram display.</summary>
    public int Peak
    {
        get
        {
            var peak = 0;
            for (var i = 0; i < 256; i++) if (_buckets[i] > peak) peak = _buckets[i];
            return peak;
        }
    }

    /// <summary>Normalised bucket heights in [0,1], scaled against the tallest bucket.</summary>
    public double[] ToNormalized()
    {
        var peak = Peak;
        var result = new double[256];
        if (peak == 0) return result;
        for (var i = 0; i < 256; i++) result[i] = _buckets[i] / (double)peak;
        return result;
    }
}

/// <summary>A colour and how much of the image it accounts for.</summary>
public readonly record struct ColorFrequency(Rgba32 Color, double Fraction)
{
    /// <summary>The share as a percentage, for display.</summary>
    public double Percent => Fraction * 100;
}

/// <summary>
/// Measurements taken from an image: per-channel histograms, luminance, average colour and a dominant colour palette.
/// </summary>
/// <remarks>
/// These are what "auto enhance" and "pick a placeholder colour" are actually made of. Computing them is a single pass
/// over the pixels, and fully transparent pixels are excluded so a logo on a transparent background does not report
/// itself as mostly black.
/// </remarks>
public sealed class ImageStatistics
{
    private ImageStatistics(Histogram red, Histogram green, Histogram blue, Histogram luminance, Histogram alpha, Rgba32 average, long opaquePixels, long totalPixels)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Luminance = luminance;
        Alpha = alpha;
        AverageColor = average;
        OpaquePixelCount = opaquePixels;
        TotalPixelCount = totalPixels;
    }

    public Histogram Red { get; }
    public Histogram Green { get; }
    public Histogram Blue { get; }

    /// <summary>Rec. 709 luminance, which is what exposure and contrast decisions should use.</summary>
    public Histogram Luminance { get; }

    public Histogram Alpha { get; }

    /// <summary>The mean colour, weighted by alpha. A reasonable placeholder or letterbox colour.</summary>
    public Rgba32 AverageColor { get; }

    /// <summary>Pixels that contributed to the colour statistics, that is, those with any opacity.</summary>
    public long OpaquePixelCount { get; }

    /// <summary>Total pixels examined.</summary>
    public long TotalPixelCount { get; }

    /// <summary>True when the image has any translucent or transparent pixels.</summary>
    public bool HasTransparency => Alpha.Minimum < 255;

    /// <summary>True when red, green and blue histograms are identical, that is, the image is grey.</summary>
    public bool IsGrayscale
    {
        get
        {
            for (var i = 0; i < 256; i++)
                if (Red[i] != Green[i] || Green[i] != Blue[i]) return false;
            return true;
        }
    }

    /// <summary>
    /// Measures an image, optionally sampling rather than reading every pixel. Sampling is exact enough for auto
    /// adjustments and dominant colours, and much faster on large images.
    /// </summary>
    /// <param name="image">The image to measure.</param>
    /// <param name="region">The region to measure, or null for the whole image.</param>
    /// <param name="maxSamples">
    /// Approximate number of pixels to read. The default of one million keeps a 24 megapixel photo under 30 ms while
    /// leaving the statistics indistinguishable from a full pass. Pass 0 to read every pixel.
    /// </param>
    public static ImageStatistics Measure(ImageBuffer image, Rectangle? region = null, int maxSamples = 1_000_000)
    {
        ArgumentNullException.ThrowIfNull(image);
        var area = region ?? image.Bounds;
        area.Intersect(image.Bounds);
        if (area.IsEmptyArea()) throw new ArgumentException("The region does not intersect the image.", nameof(region));

        // Sample on a regular grid rather than randomly, so the result is deterministic and evenly spread.
        var total = (long)area.Width * area.Height;
        var step = maxSamples > 0 && total > maxSamples ? (int)Math.Sqrt(total / (double)maxSamples) + 1 : 1;

        var red = new int[256];
        var green = new int[256];
        var blue = new int[256];
        var luminance = new int[256];
        var alpha = new int[256];
        long counted = 0, opaque = 0;
        double sumR = 0, sumG = 0, sumB = 0, sumA = 0;

        for (var y = area.Top; y < area.Bottom; y += step)
        {
            var row = image.GetRow(y);
            for (var x = area.Left; x < area.Right; x += step)
            {
                var p = row[x];
                counted++;
                alpha[p.A]++;
                if (p.A == 0) continue;

                opaque++;
                red[p.R]++;
                green[p.G]++;
                blue[p.B]++;
                // Rec. 709 luminance, matching what the human eye weights.
                var l = (int)(0.2126 * p.R + 0.7152 * p.G + 0.0722 * p.B + 0.5);
                luminance[Math.Clamp(l, 0, 255)]++;

                var weight = p.A / 255.0;
                sumR += p.R * weight;
                sumG += p.G * weight;
                sumB += p.B * weight;
                sumA += weight;
            }
        }

        var average = sumA <= 0
            ? Rgba32.Transparent
            : new Rgba32(
                (byte)Math.Clamp(sumR / sumA, 0, 255),
                (byte)Math.Clamp(sumG / sumA, 0, 255),
                (byte)Math.Clamp(sumB / sumA, 0, 255),
                (byte)Math.Clamp(sumA / counted * 255, 0, 255));

        return new ImageStatistics(
            new Histogram(red, opaque), new Histogram(green, opaque), new Histogram(blue, opaque),
            new Histogram(luminance, opaque), new Histogram(alpha, counted),
            average, opaque, counted);
    }

    /// <summary>
    /// Finds the most common colours by quantising into a colour cube and merging similar buckets. Useful for
    /// placeholder backgrounds, theming a page around an image, and choosing a letterbox colour that does not clash.
    /// </summary>
    /// <param name="image">The image to sample.</param>
    /// <param name="count">How many colours to return.</param>
    /// <param name="maxSamples">Approximate number of pixels to read.</param>
    /// <param name="ignoreNearWhiteAndBlack">
    /// Skip near-white and near-black pixels, which otherwise dominate photographs of products on white and
    /// screenshots with dark backgrounds without saying anything useful about the image.
    /// </param>
    public static IReadOnlyList<ColorFrequency> GetDominantColors(
        ImageBuffer image, int count = 5, int maxSamples = 200_000, bool ignoreNearWhiteAndBlack = false)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

        // A 5 bits per channel cube: fine enough to separate distinct colours, coarse enough that similar shades merge.
        const int bits = 5;
        const int shift = 8 - bits;
        const int size = 1 << bits;
        var counts = new Dictionary<int, (long Count, long R, long G, long B)>(1024);

        var total = (long)image.Width * image.Height;
        var step = maxSamples > 0 && total > maxSamples ? (int)Math.Sqrt(total / (double)maxSamples) + 1 : 1;
        long sampled = 0;

        for (var y = 0; y < image.Height; y += step)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < image.Width; x += step)
            {
                var p = row[x];
                if (p.A < 128) continue;
                if (ignoreNearWhiteAndBlack)
                {
                    var l = p.Luminance;
                    if (l > 244 || l < 12) continue;
                }
                var key = ((p.R >> shift) * size + (p.G >> shift)) * size + (p.B >> shift);
                var entry = counts.TryGetValue(key, out var existing) ? existing : default;
                counts[key] = (entry.Count + 1, entry.R + p.R, entry.G + p.G, entry.B + p.B);
                sampled++;
            }
        }

        if (sampled == 0) return [];
        return counts
            .OrderByDescending(e => e.Value.Count)
            .Take(count)
            // Report the average of each bucket rather than its centre, so the colour is one that is really present.
            .Select(e => new ColorFrequency(
                new Rgba32((byte)(e.Value.R / e.Value.Count), (byte)(e.Value.G / e.Value.Count), (byte)(e.Value.B / e.Value.Count)),
                e.Value.Count / (double)sampled))
            .ToList();
    }
}

/// <summary>
/// Stretches the tonal range so the darkest pixels become black and the brightest become white, ignoring a small
/// fraction of outliers at each end. The most useful single "improve this photo" operation.
/// </summary>
/// <remarks>
/// Channels are stretched together by default, which fixes exposure without changing colour. Stretching each channel
/// separately also removes colour casts, but can shift the colour of an image that is legitimately dominated by one
/// hue, so it is opt-in.
/// </remarks>
public sealed class AutoLevelsOperation : ImageOperation
{
    public AutoLevelsOperation(double clipFraction = 0.005, bool perChannel = false, float strength = 1f)
    {
        if (clipFraction is < 0 or > 0.2) throw new ArgumentOutOfRangeException(nameof(clipFraction), "Clip fraction must be between 0 and 0.2.");
        ClipFraction = clipFraction;
        PerChannel = perChannel;
        Strength = Math.Clamp(strength, 0f, 1f);
    }

    /// <summary>Fraction of pixels ignored at each end of the range. 0.005 means the darkest and brightest 0.5%.</summary>
    public double ClipFraction { get; }

    /// <summary>Stretch each channel independently, which also neutralises a colour cast.</summary>
    public bool PerChannel { get; }

    /// <summary>How far towards the fully stretched result to go, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => "Auto levels";

    public override bool IsIdentity => Strength <= 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        if (IsIdentity) return context.CanMutateSource ? source : source.Clone(context.Allocator);

        var statistics = ImageStatistics.Measure(source);
        context.ThrowIfCancellationRequested();

        var curve = PerChannel
            ? BuildPerChannelCurve(statistics)
            : BuildSharedCurve(statistics);

        return curve.Apply(source, context);
    }

    private CurveOperation BuildSharedCurve(ImageStatistics statistics)
    {
        var (low, high) = FindRange(statistics.Luminance);
        var table = BuildTable(low, high, Strength);
        return new CurveOperation(table, table, table, null, Name);
    }

    private CurveOperation BuildPerChannelCurve(ImageStatistics statistics)
    {
        var (rl, rh) = FindRange(statistics.Red);
        var (gl, gh) = FindRange(statistics.Green);
        var (bl, bh) = FindRange(statistics.Blue);
        return new CurveOperation(
            BuildTable(rl, rh, Strength),
            BuildTable(gl, gh, Strength),
            BuildTable(bl, bh, Strength),
            null, Name);
    }

    private (int Low, int High) FindRange(Histogram histogram)
    {
        var low = histogram.GetPercentile(ClipFraction);
        var high = histogram.GetPercentile(1 - ClipFraction);
        // A flat or nearly flat histogram must not be stretched into noise.
        if (high - low < 8) return (histogram.Minimum, Math.Max(histogram.Minimum + 8, histogram.Maximum));
        return (low, high);
    }

    private static float[] BuildTable(int low, int high, float strength)
    {
        var lowF = low / 255f;
        var highF = high / 255f;
        var range = MathF.Max(1f / 255f, highF - lowF);
        return CurveOperation.BuildTable(v =>
        {
            var stretched = Math.Clamp((v - lowF) / range, 0f, 1f);
            return v + (stretched - v) * strength;
        });
    }
}

/// <summary>
/// Adjusts brightness and contrast so the image's mean luminance moves towards a target. A gentler alternative to
/// <see cref="AutoLevelsOperation"/> for images that are correctly exposed but flat.
/// </summary>
public sealed class AutoContrastOperation : ImageOperation
{
    public AutoContrastOperation(float targetMean = 0.5f, float strength = 0.7f)
    {
        TargetMean = Math.Clamp(targetMean, 0.1f, 0.9f);
        Strength = Math.Clamp(strength, 0f, 1f);
    }

    /// <summary>The mean luminance to aim for, in [0,1].</summary>
    public float TargetMean { get; }

    /// <summary>How far towards the target to move, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => "Auto contrast";

    public override bool IsIdentity => Strength <= 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        if (IsIdentity) return context.CanMutateSource ? source : source.Clone(context.Allocator);

        var statistics = ImageStatistics.Measure(source);
        context.ThrowIfCancellationRequested();

        var mean = (float)(statistics.Luminance.Mean / 255.0);
        if (mean <= 0.001f || mean >= 0.999f)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);

        // Solve mean^gamma = target for gamma, so applying v^gamma moves the mean onto the target, then damp it by
        // strength. Note the exponent is gamma itself, not its reciprocal: inverting it here darkens dark images,
        // which is the opposite of what this operation is for.
        var gamma = MathF.Log(TargetMean) / MathF.Log(mean);
        gamma = 1f + (gamma - 1f) * Strength;
        gamma = Math.Clamp(gamma, 0.25f, 4f);

        var table = CurveOperation.BuildTable(v => MathF.Pow(v, gamma));
        return new CurveOperation(table, table, table, null, Name).Apply(source, context);
    }
}
