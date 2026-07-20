using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Maps data values to pixel positions for a cartesian axis and computes ticks.
/// Supports linear, logarithmic and category scales.
/// </summary>
public sealed class BitChartAxisScale
{
    public BitChartScaleOptions Options { get; }
    public BitChartScaleType Type => Options.Type;
    public double Min { get; private set; }
    public double Max { get; private set; }
    public bool Horizontal { get; }
    public List<BitChartAxisTick> Ticks { get; } = new();

    /// <summary>When set, fixes the value range exactly (used by zoom/pan).</summary>
    public (double Min, double Max)? Forced { get; set; }

    /// <summary>Computed label rotation in degrees (auto-fit for category axes).</summary>
    public double LabelRotation { get; set; }

    private double _pixelStart;
    private double _pixelEnd;
    private readonly List<string>? _categories;

    public BitChartAxisScale(BitChartScaleOptions options, bool horizontal, List<string>? categories = null)
    {
        Options = options;
        Horizontal = horizontal;
        _categories = categories;
    }

    /// <summary>Configures the value range from data, honoring min/max/suggested/beginAtZero.</summary>
    public void SetDataRange(double dataMin, double dataMax)
    {
        if (Type == BitChartScaleType.Category)
        {
            if (Forced is { } cf) { Min = cf.Min; Max = cf.Max; }
            else { Min = 0; Max = Math.Max(0, (_categories?.Count ?? 1) - 1); }
            return;
        }

        if (Forced is { } f)
        {
            Min = f.Min; Max = f.Max;
            return;
        }

        double min = dataMin, max = dataMax;
        if (double.IsInfinity(min) || double.IsInfinity(max)) { min = 0; max = 1; }

        if (Options.BeginAtZero)
        {
            if (min > 0) min = 0;
            if (max < 0) max = 0;
        }
        if (Options.SuggestedMin is { } sMin) min = Math.Min(min, sMin);
        if (Options.SuggestedMax is { } sMax) max = Math.Max(max, sMax);

        // Grace: pad the range by a fraction on each side (before explicit min/max).
        if (Options.Grace > 0 && Type != BitChartScaleType.Logarithmic)
        {
            double pad = (max - min) * Options.Grace;
            min -= pad;
            max += pad;
        }

        if (Options.Min is { } oMin) min = oMin;
        if (Options.Max is { } oMax) max = oMax;

        if (Math.Abs(max - min) < 1e-9)
        {
            // Avoid a zero-height range.
            if (Math.Abs(max) < 1e-9) { min = -1; max = 1; }
            else { min -= Math.Abs(min) * 0.1; max += Math.Abs(max) * 0.1; }
        }

        if (Type == BitChartScaleType.Logarithmic)
        {
            if (min <= 0) min = dataMin > 0 ? dataMin : 1;
            Min = min; Max = max;
        }
        else
        {
            Min = min; Max = max;
        }
    }

    /// <summary>Sets the pixel range. For horizontal axes pixels increase left→right; for vertical, top→bottom (so we invert).</summary>
    public void SetPixelRange(double start, double end)
    {
        _pixelStart = start;
        _pixelEnd = end;
        BuildTicks();
    }

    public double PixelFor(double value)
    {
        double t = NormalizedPosition(value);
        if (Options.Reverse) t = 1 - t;
        // For vertical axes the caller passes start=bottom pixel, end=top pixel.
        return _pixelStart + (_pixelEnd - _pixelStart) * t;
    }

    /// <summary>Pixel position for a category index (category scales).</summary>
    public double PixelForIndex(int index, bool centered)
    {
        if (Type != BitChartScaleType.Category)
            return PixelFor(index);

        double lo = Min, hi = Max;
        double span = hi - lo;
        double t;
        if (Options.Offset || centered)
            t = (index - lo + 0.5) / (span + 1);
        else
            t = span <= 0 ? 0.5 : (index - lo) / span;
        if (Options.Reverse) t = 1 - t;
        return _pixelStart + (_pixelEnd - _pixelStart) * t;
    }

    /// <summary>Width in pixels of one category band.</summary>
    public double BandWidth()
    {
        double count = Type == BitChartScaleType.Category
            ? Math.Max(1, Max - Min + 1)
            : Math.Max(1, _categories?.Count ?? 1);
        return Math.Abs(_pixelEnd - _pixelStart) / count;
    }

    private double NormalizedPosition(double value)
    {
        if (Type == BitChartScaleType.Logarithmic)
        {
            double lo = Math.Log10(Math.Max(Min, 1e-9));
            double hi = Math.Log10(Math.Max(Max, 1e-9));
            double v = Math.Log10(Math.Max(value, 1e-9));
            return (hi - lo) == 0 ? 0 : (v - lo) / (hi - lo);
        }
        return (Max - Min) == 0 ? 0 : (value - Min) / (Max - Min);
    }

    private void BuildTicks()
    {
        Ticks.Clear();
        switch (Type)
        {
            case BitChartScaleType.Category:
                BuildCategoryTicks();
                break;
            case BitChartScaleType.Logarithmic:
                BuildLogTicks();
                break;
            case BitChartScaleType.Time:
                BuildTimeTicks();
                break;
            default:
                BuildLinearTicks();
                break;
        }
    }

    private void BuildTimeTicks()
    {
        int maxTicks = Options.Ticks.Count ?? Options.Ticks.MaxTicksLimit ?? 11;
        foreach (var (value, label) in BitChartTimeAxis.Ticks(Min, Max, Options.TimeUnit, Options.TimeFormat, maxTicks))
            Ticks.Add(new BitChartAxisTick(value, label, PixelFor(value)));
    }

    private void BuildCategoryTicks()
    {
        if (_categories is null) return;
        int n = _categories.Count;
        int start = Math.Max(0, (int)Math.Ceiling(Min - 1e-9));
        int end = Math.Min(n - 1, (int)Math.Floor(Max + 1e-9));
        if (end < start) return;
        int visible = end - start + 1;
        int maxLabels = Options.Ticks.MaxTicksLimit ?? visible;
        int skip = Options.Ticks.AutoSkip && visible > maxLabels ? (int)Math.Ceiling((double)visible / maxLabels) : 1;
        for (int i = start; i <= end; i++)
        {
            if ((i - start) % skip != 0 && i != end) continue;
            double px = PixelForIndex(i, Options.Offset);
            Ticks.Add(new BitChartAxisTick(i, _categories[i], px));
        }
    }

    private void BuildLinearTicks()
    {
        int maxTicks = Options.Ticks.Count ?? Options.Ticks.MaxTicksLimit ?? 11;
        maxTicks = Math.Max(2, maxTicks);

        double range = Max - Min;
        double rawStep = Options.Ticks.StepSize ?? NiceNumber(range / (maxTicks - 1), round: true);
        if (rawStep <= 0) rawStep = 1;

        double niceMin, niceMax;
        if (Forced is not null)
        {
            // Keep the exact zoom range; only choose a step for label placement.
            niceMin = Min;
            niceMax = Max;
        }
        else
        {
            niceMin = Math.Floor(Min / rawStep) * rawStep;
            niceMax = Math.Ceiling(Max / rawStep) * rawStep;
            if (Options.Min is { } omin0) niceMin = omin0;
            if (Options.Max is { } omax0) niceMax = omax0;
            Min = niceMin;
            Max = niceMax;
        }

        int decimals = DecimalsFor(rawStep);
        for (double v = niceMin; v <= niceMax + rawStep * 0.5; v += rawStep)
        {
            double val = Math.Round(v, 8);
            if (val < Min - 1e-9 || val > Max + 1e-9) continue;
            Ticks.Add(new BitChartAxisTick(val, FormatValue(val, decimals), PixelFor(val)));
        }
    }

    private void BuildLogTicks()
    {
        double lo = Math.Floor(Math.Log10(Math.Max(Min, 1e-9)));
        double hi = Math.Ceiling(Math.Log10(Math.Max(Max, 1e-9)));
        for (double e = lo; e <= hi; e++)
        {
            double major = Math.Pow(10, e);
            if (major >= Min - 1e-9 && major <= Max + 1e-9)
                Ticks.Add(new BitChartAxisTick(major, FormatValue(major, 0), PixelFor(major)));

            // Minor ticks at 2..9 × 10^e.
            if (Options.Ticks.Display)
                for (int m = 2; m <= 9; m++)
                {
                    double minor = m * major;
                    if (minor < Min - 1e-9 || minor > Max + 1e-9) continue;
                    Ticks.Add(new BitChartAxisTick(minor, "", PixelFor(minor), Minor: true));
                }
        }
        if (Ticks.Count == 0)
        {
            Ticks.Add(new BitChartAxisTick(Min, FormatValue(Min, 0), PixelFor(Min)));
            Ticks.Add(new BitChartAxisTick(Max, FormatValue(Max, 0), PixelFor(Max)));
        }
    }

    public string FormatValue(double value, int decimals)
    {
        if (Options.Ticks.Callback is { } cb)
            return cb(value, Ticks.Count);
        if (Options.Ticks.Precision is { } p) decimals = p;
        string s = value.ToString("N" + Math.Max(0, decimals), CultureInfo.InvariantCulture);
        return $"{Options.Ticks.Prefix}{s}{Options.Ticks.Suffix}";
    }

    private static int DecimalsFor(double step)
    {
        if (step >= 1) return 0;
        return (int)Math.Ceiling(-Math.Log10(step));
    }

    /// <summary>Returns a "nice" number approximately equal to <paramref name="value"/>.</summary>
    public static double NiceNumber(double value, bool round)
    {
        if (value <= 0) return 1;
        double exp = Math.Floor(Math.Log10(value));
        double frac = value / Math.Pow(10, exp);
        double niceFrac = round
            ? frac < 1.5 ? 1 : frac < 3 ? 2 : frac < 7 ? 5 : 10
            : frac <= 1 ? 1 : frac <= 2 ? 2 : frac <= 5 ? 5 : 10;
        return niceFrac * Math.Pow(10, exp);
    }
}
