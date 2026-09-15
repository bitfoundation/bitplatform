using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Fits a trend line through a cartesian dataset - a least-squares regression, a trailing moving
/// average or the series mean - and draws it over the chart, mirroring what the charting world ships
/// as a trendline plugin. The fit is computed from the same values the chart plots, so a dataset
/// hidden through the legend takes its trend line with it.
/// </summary>
public sealed class BitChartTrendlinePlugin : IBitChartPlugin
{
    public string Id => "trendline";

    public List<BitChartTrendline> Trendlines { get; } = new();

    public BitChartTrendlinePlugin() { }
    public BitChartTrendlinePlugin(params BitChartTrendline[] trendlines) => Trendlines.AddRange(trendlines);

    public void BeforeDatasetsDraw(BitChartPluginContext ctx)
    {
        foreach (var t in Trendlines.Where(t => t.DrawBehindDatasets)) Draw(ctx, t, behind: true);
    }

    public void AfterDatasetsDraw(BitChartPluginContext ctx)
    {
        foreach (var t in Trendlines.Where(t => !t.DrawBehindDatasets)) Draw(ctx, t, behind: false);
    }

    private static void Draw(BitChartPluginContext ctx, BitChartTrendline trend, bool behind)
    {
        if (!ctx.IsCartesian || ctx.Plot is not { } plot) return;

        var datasets = ctx.Config.Data.Datasets;
        if (trend.DatasetIndex < 0 || trend.DatasetIndex >= datasets.Count) return;
        var ds = datasets[trend.DatasetIndex];
        if (!ctx.IsDatasetVisible(trend.DatasetIndex)) return;

        // (x, y) in data coordinates: an index for a category axis, the point's own x otherwise.
        var samples = new List<(double X, double Y)>();
        if (ds.Points is { } points)
        {
            foreach (var p in points.OrderBy(p => p.X)) samples.Add((p.X, p.Y));
        }
        else
        {
            for (int i = 0; i < ds.Data.Count; i++)
                if (ds.Data[i] is { } v) samples.Add((i, v));
        }
        if (samples.Count < 2) return;

        bool byIndex = ds.Points is null && ctx.IndexIsCategory;
        double Px(double x) => byIndex ? ctx.XForIndex((int)Math.Round(x)) : ctx.XForValue(x);
        double Py(double y) => ctx.YForValue(y, ds.YAxisID);

        var fitted = trend.Kind switch
        {
            BitChartTrendlineKind.MovingAverage => MovingAverage(samples, trend.Period),
            BitChartTrendlineKind.Average => Flat(samples),
            _ => Regression(samples, trend.Extend, ctx, byIndex, plot)
        };
        if (fitted.Count < 2) return;

        string color = trend.Color ?? BitChartColorUtil.WithAlpha(
            ds.BorderColor ?? ds.BackgroundColor ?? BitChartColorUtil.Palette(trend.DatasetIndex), 0.85);

        var d = new StringBuilder();
        for (int i = 0; i < fitted.Count; i++)
        {
            double px = fitted[i].Fixed ? fitted[i].X : Px(fitted[i].X);
            double py = Py(fitted[i].Y);
            d.Append(i == 0 ? "M " : " L ").Append(BitChartSvg.N(px)).Append(' ').Append(BitChartSvg.N(py));
        }

        Action<BitChartSvgNode> add = behind ? ctx.AddBehind : ctx.AddFront;
        add(new BitChartSvgPath
        {
            D = d.ToString(),
            Fill = "none",
            Stroke = color,
            StrokeWidth = trend.LineWidth,
            Dash = BitChartSvg.Dash(trend.Dash),
            LineCap = "round",
            LineJoin = "round"
        });

        if (string.IsNullOrEmpty(trend.Label)) return;

        var end = fitted[^1];
        double lx = end.Fixed ? end.X : Px(end.X);
        double ly = Py(end.Y);
        double w = BitChartTextMeasure.Width(trend.Label, trend.LabelFont.Size, trend.LabelFont.Weight) + 12;
        double h = trend.LabelFont.LineHeightPx + 6;
        // Pinned inside the plot so a fit that ends at the right edge keeps its pill readable.
        double left = Math.Clamp(lx - w, plot.Left, Math.Max(plot.Left, plot.Right - w));
        double top = Math.Clamp(ly - h / 2, plot.Top, Math.Max(plot.Top, plot.Bottom - h));

        add(new BitChartSvgRect
        {
            X = left, Y = top, Width = w, Height = h, Rx = 4,
            Fill = trend.LabelBackground ?? color
        });
        add(new BitChartSvgText
        {
            X = left + w / 2, Y = top + h / 2, Text = trend.Label!, Fill = trend.LabelColor,
            FontFamily = trend.LabelFont.Family, FontSize = trend.LabelFont.Size, FontWeight = trend.LabelFont.Weight,
            Anchor = "middle", Baseline = "central"
        });
    }

    /// <summary>
    /// A fitted vertex. <c>Fixed</c> marks a point whose X is already a pixel - the regression uses it
    /// to reach the plot edges, which have no data coordinate to convert from.
    /// </summary>
    private readonly record struct Vertex(double X, double Y, bool Fixed = false);

    /// <summary>Ordinary least-squares fit, evaluated at the two ends of the run it covers.</summary>
    private static List<Vertex> Regression(List<(double X, double Y)> samples, bool extend,
        BitChartPluginContext ctx, bool byIndex, BitChartArea plot)
    {
        int n = samples.Count;
        double sx = 0, sy = 0, sxy = 0, sxx = 0;
        foreach (var (x, y) in samples) { sx += x; sy += y; sxy += x * y; sxx += x * x; }
        double denom = n * sxx - sx * sx;
        // A vertical run of samples has no least-squares line; the mean is the honest answer for it.
        if (Math.Abs(denom) < 1e-12) return Flat(samples);

        double slope = (n * sxy - sx * sy) / denom;
        double intercept = (sy - slope * sx) / n;

        double x0 = samples[0].X, x1 = samples[^1].X;
        if (!extend)
            return [new Vertex(x0, intercept + slope * x0), new Vertex(x1, intercept + slope * x1)];

        // Extending means walking the line out to the plot edges, so the endpoints are converted the
        // other way round: from a pixel back to the data coordinate the fit is expressed in.
        double px0 = byIndex ? ctx.XForIndex((int)Math.Round(x0)) : ctx.XForValue(x0);
        double px1 = byIndex ? ctx.XForIndex((int)Math.Round(x1)) : ctx.XForValue(x1);
        if (Math.Abs(px1 - px0) < 1e-9)
            return [new Vertex(x0, intercept + slope * x0), new Vertex(x1, intercept + slope * x1)];

        double perPixel = (x1 - x0) / (px1 - px0);
        double left = px0 < px1 ? plot.Left : plot.Right;
        double right = px0 < px1 ? plot.Right : plot.Left;
        double dataAtLeft = x0 + (left - px0) * perPixel;
        double dataAtRight = x0 + (right - px0) * perPixel;
        return
        [
            new Vertex(left, intercept + slope * dataAtLeft, Fixed: true),
            new Vertex(right, intercept + slope * dataAtRight, Fixed: true)
        ];
    }

    /// <summary>Trailing simple moving average; the first points average what there is so far.</summary>
    private static List<Vertex> MovingAverage(List<(double X, double Y)> samples, int period)
    {
        int p = Math.Max(2, period);
        var result = new List<Vertex>(samples.Count);
        double sum = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            sum += samples[i].Y;
            if (i >= p) sum -= samples[i - p].Y;
            result.Add(new Vertex(samples[i].X, sum / Math.Min(i + 1, p)));
        }
        return result;
    }

    /// <summary>A flat line at the mean of the series.</summary>
    private static List<Vertex> Flat(List<(double X, double Y)> samples)
    {
        double mean = samples.Average(s => s.Y);
        return [new Vertex(samples[0].X, mean), new Vertex(samples[^1].X, mean)];
    }
}
