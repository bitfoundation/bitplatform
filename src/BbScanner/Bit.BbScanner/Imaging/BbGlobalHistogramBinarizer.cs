namespace Bit.BbScanner;

/// <summary>
/// Picks one global black point from a coarse luminance histogram.
/// </summary>
/// <remarks>
/// <para>
/// The histogram has 32 buckets, which is enough to separate the ink peak from the paper peak
/// while keeping the accumulation loop cache friendly. The black point is placed in the widest
/// valley between the two tallest peaks, weighted by the square of their separation so that a
/// tall but nearby bucket does not win over the true paper peak.
/// </para>
/// <para>
/// This binariser is the right choice for linear symbologies: it is roughly four times cheaper
/// than <see cref="BbHybridBinarizer"/> and linear codes are decoded from individual rows where a
/// per-row global threshold already adapts to local lighting.
/// </para>
/// </remarks>
public sealed class BbGlobalHistogramBinarizer : IBbBinarizer
{
    private const int LuminanceBits = 5;
    private const int LuminanceBuckets = 1 << LuminanceBits;
    private const int LuminanceShift = 8 - LuminanceBits;

    private readonly int[] _buckets = new int[LuminanceBuckets];

    /// <summary>A shared instance. It carries a scratch histogram, so it is not thread safe.</summary>
    public static BbGlobalHistogramBinarizer Shared { get; } = new();

    /// <inheritdoc />
    public bool TryGetBlackRow(in BbLuminanceView source, int y, BbBitRow row)
    {
        var width = source.Width;
        row.Reset(width);

        var pixels = source.GetRow(y);
        var buckets = _buckets;
        Array.Clear(buckets);

        for (var x = 0; x < width; x++)
        {
            buckets[pixels[x] >> LuminanceShift]++;
        }

        if (!TryEstimateBlackPoint(buckets, out var blackPoint))
        {
            return false;
        }

        if (width < 3)
        {
            return false;
        }

        // Compare against the neighbours as well: this removes the single pixel speckle that a
        // hard threshold produces on noisy sensors, at no measurable cost.
        var left = pixels[0];
        var center = pixels[1];
        for (var x = 1; x < width - 1; x++)
        {
            var right = pixels[x + 1];
            if ((((center * 4) - left - right) / 2) < blackPoint)
            {
                row[x] = true;
            }

            left = center;
            center = right;
        }

        return true;
    }

    /// <inheritdoc />
    public BbBitMatrix? GetBlackMatrix(in BbLuminanceView source)
    {
        var width = source.Width;
        var height = source.Height;
        var buckets = _buckets;
        Array.Clear(buckets);

        // Sample a fixed number of rows spread over the image rather than every row: the global
        // black point does not get better from more samples, but the pass gets much cheaper.
        const int SampleRows = 5;
        for (var i = 1; i < SampleRows; i++)
        {
            var y = height * i / SampleRows;
            var pixels = source.GetRow(y);
            var right = (width * 4) / 5;
            for (var x = width / 5; x < right; x++)
            {
                buckets[pixels[x] >> LuminanceShift]++;
            }
        }

        if (!TryEstimateBlackPoint(buckets, out var blackPoint))
        {
            return null;
        }

        var matrix = new BbBitMatrix(width, height);
        for (var y = 0; y < height; y++)
        {
            var pixels = source.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                if (pixels[x] < blackPoint)
                {
                    matrix[x, y] = true;
                }
            }
        }

        return matrix;
    }

    private static bool TryEstimateBlackPoint(ReadOnlySpan<int> buckets, out int blackPoint)
    {
        blackPoint = 0;

        var numBuckets = buckets.Length;
        var maxBucketCount = 0;
        var firstPeak = 0;
        var firstPeakSize = 0;
        for (var i = 0; i < numBuckets; i++)
        {
            if (buckets[i] > firstPeakSize)
            {
                firstPeak = i;
                firstPeakSize = buckets[i];
            }

            if (buckets[i] > maxBucketCount)
            {
                maxBucketCount = buckets[i];
            }
        }

        var secondPeak = 0;
        var secondPeakScore = 0;
        for (var i = 0; i < numBuckets; i++)
        {
            var distance = i - firstPeak;
            var score = buckets[i] * distance * distance;
            if (score > secondPeakScore)
            {
                secondPeak = i;
                secondPeakScore = score;
            }
        }

        // No second peak at all means every sample landed in one bucket: a blank, flat line with
        // no ink on it. Reporting a black point anyway would send every row decoder over an
        // empty row, which is the commonest frame a scanner sees.
        if (secondPeakScore == 0)
        {
            return false;
        }

        if (firstPeak > secondPeak)
        {
            (firstPeak, secondPeak) = (secondPeak, firstPeak);
        }

        // Two peaks that sit on top of each other mean a flat image: there is no barcode to find.
        if (secondPeak - firstPeak <= numBuckets / 16)
        {
            return false;
        }

        var bestValley = secondPeak - 1;
        var bestValleyScore = -1;
        for (var i = secondPeak - 1; i > firstPeak; i--)
        {
            var fromFirst = i - firstPeak;
            var score = fromFirst * fromFirst * (secondPeak - i) * (maxBucketCount - buckets[i]);
            if (score > bestValleyScore)
            {
                bestValley = i;
                bestValleyScore = score;
            }
        }

        blackPoint = bestValley << LuminanceShift;
        return true;
    }
}
