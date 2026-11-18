namespace Bit.Bmage;

/// <summary>
/// A 256-bucket histogram for one channel, with the derived values that adjustments actually need.
/// </summary>
public sealed class BmageHistogram
{
    private readonly int[] _buckets;

    internal BmageHistogram(int[] buckets, long total)
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
