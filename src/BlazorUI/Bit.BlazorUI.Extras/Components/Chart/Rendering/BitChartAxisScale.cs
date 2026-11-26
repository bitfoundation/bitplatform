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

    /// <summary>Culture used to format tick labels. Defaults to the invariant culture.</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>The pixel range currently mapped onto, as last passed to <see cref="SetPixelRange"/>.</summary>
    public double PixelStart => _pixelStart;
    /// <inheritdoc cref="PixelStart"/>
    public double PixelEnd => _pixelEnd;

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

    /// <summary>
    /// The most ticks that fit along the axis without their labels colliding. Value axes read labels
    /// perpendicular to the axis, so the limit follows the line height; along a horizontal axis it
    /// follows a conservative label width instead.
    /// </summary>
    private int FitTickLimit(int requested)
    {
        if (!Options.AutoSkipTicks) return requested;
        // A single-category axis - or an explicit tick count of one - asks for fewer than the two the
        // fit is floored at, and there is nothing to thin out there anyway.
        if (requested < 2) return requested;
        double length = Math.Abs(_pixelEnd - _pixelStart);
        if (length <= 0) return requested;
        double perTick = Horizontal
            ? Math.Max(24, Options.Ticks.Font.Size * 3.2)   // room for a short numeric label
            : Options.Ticks.Font.LineHeightPx * 1.6;        // room for one line of text
        int fits = (int)Math.Floor(length / perTick) + 1;
        return Math.Clamp(fits, 2, requested);
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
        int maxTicks = FitTickLimit(Options.Ticks.Count ?? Options.Ticks.MaxTicksLimit ?? 11);
        foreach (var (value, label) in BitChartTimeAxis.Ticks(Min, Max, Options.TimeUnit, Options.TimeFormat, maxTicks, Culture))
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
        int maxLabels = FitTickLimit(Options.Ticks.MaxTicksLimit ?? visible);
        if (Options.Ticks.AutoSkip is false || visible <= maxLabels)
        {
            for (int i = start; i <= end; i++)
                AddCategoryTick(i);
            return;
        }

        // Labels need at least `skip` categories between them. Every skip-th label is kept from the first one, so the
        // labels fall on a regular cadence and stay where they are while the end of the range moves (a zoom from the
        // left edge, say). The last label is kept too, and when it would land closer than `skip` to the stride label
        // before it - whenever the span is not a multiple of skip - that stride label gives way rather than being drawn
        // over. A limit of one label keeps the first label only: there is no room for the last one `skip` away from it.
        int skip = (int)Math.Ceiling((double)visible / Math.Max(1, maxLabels));
        int lastStride = start + (end - start) / skip * skip;
        for (int i = start; i <= lastStride; i += skip)
        {
            if (i != start && i != end && end - i < skip) continue;
            AddCategoryTick(i);
        }
        if (lastStride != end && end - start >= skip)
            AddCategoryTick(end);
    }

    private void AddCategoryTick(int index)
    {
        string label = Options.Ticks.Callback is { } cb ? cb(index, LabelIndex()) : _categories![index];
        Ticks.Add(new BitChartAxisTick(index, label, PixelForIndex(index, Options.Offset)));
    }

    private void BuildLinearTicks()
    {
        int maxTicks = FitTickLimit(Options.Ticks.Count ?? Options.Ticks.MaxTicksLimit ?? 11);
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

        // A step far smaller than the range (an explicit StepSize of 1 over millions, say) would
        // otherwise generate ticks until the browser gives up, so the emitted count is bounded.
        int limit = Math.Max(maxTicks * 4, 64);
        if ((niceMax - niceMin) / rawStep > limit)
            rawStep = (niceMax - niceMin) / limit;

        int decimals = DecimalsFor(rawStep);
        for (double v = niceMin; v <= niceMax + rawStep * 0.5; v += rawStep)
        {
            double val = Math.Round(v, 8);
            if (val < Min - 1e-9 || val > Max + 1e-9) continue;
            Ticks.Add(new BitChartAxisTick(val, FormatValue(val, decimals), PixelFor(val)));
            if (Ticks.Count > limit) break;
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

    /// <summary>
    /// A single value - a mark placed on the axis rather than one of its ticks - spelled the way a reader of the axis
    /// knows it: a date on a time axis, the plain number in the chart's culture otherwise.
    /// </summary>
    internal string FormatPoint(double value)
        => Type == BitChartScaleType.Time
            ? BitChartTimeAxis.Format(value, Min, Max, Options.TimeUnit, Options.TimeFormat, Culture)
            : value.ToString(Culture);

    public string FormatValue(double value, int decimals)
    {
        if (Options.Ticks.Callback is { } cb)
            return cb(value, LabelIndex());
        if (Options.Ticks.Precision is { } p) decimals = p;
        string format = Options.Ticks.Format ?? "N" + Math.Max(0, decimals);
        string s = value.ToString(format, Culture);
        return $"{Options.Ticks.Prefix}{s}{Options.Ticks.Suffix}";
    }

    /// <summary>
    /// The index a tick callback is given for the next tick: its position among the labelled ticks built so far,
    /// leaving out the minor ticks of a logarithmic axis, which carry no label and never reach the callback.
    /// </summary>
    private int LabelIndex()
    {
        int index = 0;
        foreach (var t in Ticks)
            if (!t.Minor) index++;
        return index;
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
