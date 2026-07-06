
namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private bool IsVertical => _options.IndexAxis == BitChartIndexAxis.X;

    private void RenderCartesian(BitChartScene scene)
    {
        var area = ContentArea();

        // Identify which scale ids are x-axes (datasets' XAxisID plus the default "x").
        var xIds = new HashSet<string>(_data.Datasets.Select(d => string.IsNullOrEmpty(d.XAxisID) ? "x" : d.XAxisID)) { "x" };

        var indexScaleOpts = _options.Scales["x"];
        bool indexIsCategory = indexScaleOpts.Type == BitChartScaleType.Category;

        // Value (y) axes used by datasets.
        var leftAxes = new List<BitChartAxisScale>();
        var rightAxes = new List<BitChartAxisScale>();
        var valueScales = new Dictionary<string, BitChartAxisScale>();

        foreach (var (id, so) in _options.Scales)
        {
            if (xIds.Contains(id) || so.Type == BitChartScaleType.RadialLinear) continue;
            var (mn, mx) = ComputeValueExtent(id);
            var scale = new BitChartAxisScale(so, horizontal: !IsVertical);
            if (so.Type != BitChartScaleType.Category && _state.AxisRanges.TryGetValue(id, out var ov))
                scale.Forced = ov;
            scale.SetDataRange(mn, mx);
            scale.SetPixelRange(0, 100); // provisional, for tick labels
            valueScales[id] = scale;
            if (so.Type != BitChartScaleType.Category) scene.ZoomableAxes.Add(id);
            if (!so.Display) continue;
            if ((so.Position ?? BitChartPosition.Left) == BitChartPosition.Right) rightAxes.Add(scale);
            else leftAxes.Add(scale);
        }

        // X axes ("x" is the primary index scale; others are secondary, point-bound).
        var xScales = new Dictionary<string, BitChartAxisScale>();
        var bottomXAxes = new List<BitChartAxisScale>();
        var topXAxes = new List<BitChartAxisScale>();
        BitChartAxisScale indexScale = default!;

        foreach (var id in xIds.OrderBy(s => s == "x" ? 0 : 1))
        {
            var so = _options.Scales[id];
            BitChartAxisScale xs;
            if (id == "x" && indexIsCategory)
            {
                xs = new BitChartAxisScale(so, horizontal: IsVertical, categories: _data.Labels);
                if (_state.AxisRanges.TryGetValue("x", out var cov)) xs.Forced = cov;
                xs.SetDataRange(0, Math.Max(0, _data.Labels.Count - 1));
            }
            else
            {
                var (mn, mx) = id == "x" ? ComputeIndexExtent() : ComputeXExtent(id);
                xs = new BitChartAxisScale(so, horizontal: IsVertical);
                if (_state.AxisRanges.TryGetValue(id, out var ov)) xs.Forced = ov;
                xs.SetDataRange(mn, mx);
            }
            xs.SetPixelRange(0, 100);
            xScales[id] = xs;
            scene.ZoomableAxes.Add(id);
            if (id == "x") indexScale = xs;
            if (so.Display)
            {
                if ((so.Position ?? BitChartPosition.Bottom) == BitChartPosition.Top) topXAxes.Add(xs);
                else bottomXAxes.Add(xs);
            }
        }

        // ---- Reserve space for axes ----
        double leftReserve = leftAxes.Sum(a => ReserveValueAxis(a));
        double rightReserve = rightAxes.Sum(a => ReserveValueAxis(a));

        // Auto-rotate category labels on the bottom (x) axis when they don't fit.
        if (IsVertical && indexIsCategory && indexScaleOpts.Display && indexScaleOpts.Ticks.Display)
        {
            double availW = area.Width - leftReserve - rightReserve;
            indexScale.LabelRotation = ComputeIndexLabelRotation(indexScale, availW);
        }

        double bottomReserve = IsVertical ? bottomXAxes.Sum(ReserveIndexAxis) : ReserveIndexAxis(indexScale);
        double topReserve = IsVertical ? topXAxes.Sum(ReserveIndexAxis) : 0;

        var plot = new BitChartArea(area.Left + leftReserve, area.Top + topReserve, area.Right - rightReserve, area.Bottom - bottomReserve);

        // ---- Final pixel ranges ----
        if (IsVertical)
        {
            foreach (var s in xScales.Values) s.SetPixelRange(plot.Left, plot.Right);
            foreach (var s in valueScales.Values) s.SetPixelRange(plot.Bottom, plot.Top);
        }
        else
        {
            indexScale.SetPixelRange(plot.Top, plot.Bottom);
            foreach (var s in valueScales.Values) s.SetPixelRange(plot.Left, plot.Right);
        }

        // ---- Grid + axes ----
        DrawGrid(scene, plot, indexScale, valueScales, leftAxes, rightAxes);

        // Secondary x axes (display only; stacked outside the plot, no chart-area grid).
        if (IsVertical)
        {
            double belowOffset = ReserveIndexAxis(indexScale);
            foreach (var xs in bottomXAxes)
            {
                if (ReferenceEquals(xs, indexScale)) continue;
                DrawSecondaryXAxis(scene, plot, xs, plot.Bottom + belowOffset, atBottom: true);
                belowOffset += ReserveIndexAxis(xs);
            }
            double aboveOffset = 0;
            foreach (var xs in topXAxes)
            {
                if (ReferenceEquals(xs, indexScale)) continue;
                aboveOffset += ReserveIndexAxis(xs);
                DrawSecondaryXAxis(scene, plot, xs, plot.Top - aboveOffset, atBottom: false);
            }
        }

        scene.PlotArea = plot;

        scene.AxisRanges["x"] = (indexScale.Min, indexScale.Max);
        foreach (var (id, s) in xScales) scene.AxisRanges[id] = (s.Min, s.Max);
        foreach (var (id, s) in valueScales) scene.AxisRanges[id] = (s.Min, s.Max);

        var ctx = new BitChartPluginContext
        {
            Scene = scene,
            Config = _config,
            Plot = plot,
            IsCartesian = true,
            IndexScale = indexScale,
            ValueScales = valueScales,
            IndexIsCategory = indexIsCategory
        };
        foreach (var plugin in _options.Plugins.Custom) plugin.BeforeDatasetsDraw(ctx);

        BitChartAxisScale XScaleFor(BitChartDataset ds)
            => xScales.TryGetValue(string.IsNullOrEmpty(ds.XAxisID) ? "x" : ds.XAxisID, out var s) ? s : indexScale;

        // ---- Datasets (respect Order) ----
        var ordered = _data.Datasets
            .Select((d, i) => (d, i))
            .OrderBy(t => t.d.Order)
            .ToList();

        // Bars first (so lines/points draw on top), grouped/stacked layout.
        var barItems = ordered.Where(t => EffectiveType(t.d) == BitChartType.Bar && !IsHidden(t.i, t.d)).ToList();
        DrawBars(scene, plot, indexScale, valueScales, barItems, indexIsCategory);
        bool centered = barItems.Count > 0;
        scene.HasBars = barItems.Count > 0;
        scene.HorizontalBars = !IsVertical;

        // Stacked line datasets are drawn together (cumulative areas).
        var stackedLines = ordered
            .Where(t => EffectiveType(t.d) == BitChartType.Line && !IsHidden(t.i, t.d)
                        && valueScales.TryGetValue(t.d.YAxisID, out var sc) && sc.Options.Stacked
                        && t.d.Points is null)
            .ToList();
        if (stackedLines.Count > 0)
            DrawStackedAreas(scene, plot, indexScale, valueScales, stackedLines, indexIsCategory, centered);
        var stackedSet = stackedLines.Select(t => t.i).ToHashSet();

        foreach (var (ds, i) in ordered)
        {
            if (IsHidden(i, ds)) continue;
            var type = EffectiveType(ds);
            var vScale = valueScales.TryGetValue(ds.YAxisID, out var vs) ? vs : valueScales.Values.First();
            var xScale = XScaleFor(ds);
            switch (type)
            {
                case BitChartType.Line when !stackedSet.Contains(i):
                    DrawLine(scene, plot, xScale, vScale, ds, i, indexIsCategory, centered);
                    break;
                case BitChartType.Scatter:
                    DrawScatter(scene, plot, xScale, vScale, ds, i, bubble: false);
                    break;
                case BitChartType.Bubble:
                    DrawScatter(scene, plot, xScale, vScale, ds, i, bubble: true);
                    break;
            }
        }

        foreach (var plugin in _options.Plugins.Custom) plugin.AfterDatasetsDraw(ctx);
    }

    private BitChartType EffectiveType(BitChartDataset ds) => ds.Type ?? _config.Type;
    private bool IsHidden(int i, BitChartDataset ds) => ds.Hidden || _state.IsDatasetHidden(i);

    private double ReserveIndexAxis(BitChartAxisScale scale)
    {
        var o = scale.Options;
        if (!o.Display) return 0;
        double h = 0;
        if (o.Grid.DrawTicks) h += o.Grid.TickLength;
        if (o.Ticks.Display)
        {
            double rot = Math.Abs(scale.LabelRotation);
            if (rot < 1e-3)
            {
                h += o.Ticks.Font.LineHeightPx + o.Ticks.Padding;
            }
            else
            {
                double maxLabel = 0;
                foreach (var t in scale.Ticks)
                    maxLabel = Math.Max(maxLabel, BitChartTextMeasure.Width(t.Label, o.Ticks.Font.Size));
                double rad = rot * Math.PI / 180;
                h += maxLabel * Math.Sin(rad) + o.Ticks.Font.Size * Math.Cos(rad) + o.Ticks.Padding;
            }
        }
        if (o.Title.Display) h += o.Title.Font.LineHeightPx + o.Title.Padding.Vertical;
        return h;
    }

    /// <summary>Computes an auto label rotation (degrees) so category labels fit their band width.</summary>
    private static double ComputeIndexLabelRotation(BitChartAxisScale scale, double availWidth)
    {
        var tk = scale.Options.Ticks;
        if (Math.Abs(tk.Rotation) > 1e-3) return tk.Rotation;     // explicit rotation wins
        if (tk.MaxRotation <= 0 || scale.Ticks.Count == 0) return 0;

        double maxLabel = 0;
        foreach (var t in scale.Ticks)
            maxLabel = Math.Max(maxLabel, BitChartTextMeasure.Width(t.Label, tk.Font.Size, tk.Font.Weight));
        if (maxLabel <= 0) return 0;

        double band = availWidth / Math.Max(1, scale.Ticks.Count);
        if (maxLabel <= band * 0.95) return 0;   // fits horizontally

        // Rotate just enough so the horizontal footprint fits the band, clamped to limits.
        double ratio = Math.Clamp(band / maxLabel, -1, 1);
        double needed = Math.Acos(ratio) * 180 / Math.PI;
        return Math.Clamp(needed, Math.Max(tk.MinRotation, 1), tk.MaxRotation);
    }

    private double ReserveValueAxis(BitChartAxisScale scale)
    {
        var o = scale.Options;
        double maxLabel = 0;
        if (o.Ticks.Display)
            foreach (var t in scale.Ticks)
                maxLabel = Math.Max(maxLabel, EstimateTextWidth(t.Label, o.Ticks.Font.Size));
        double w = maxLabel + o.Ticks.Padding;
        if (o.Grid.DrawTicks) w += o.Grid.TickLength;
        if (o.Title.Display) w += o.Title.Font.LineHeightPx + o.Title.Padding.Horizontal;
        return w + 2;
    }

    // ---- extents ----

    private (double, double) ComputeValueExtent(string axisId)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        var scaleOpts = _options.Scales[axisId];

        if (scaleOpts.Stacked && scaleOpts.Stacked100)
            return (0, 100);

        if (scaleOpts.Stacked)
        {
            var posSums = new Dictionary<int, double>();
            var negSums = new Dictionary<int, double>();
            for (int d = 0; d < _data.Datasets.Count; d++)
            {
                var ds = _data.Datasets[d];
                if (ds.YAxisID != axisId || IsHidden(d, ds)) continue;
                for (int i = 0; i < ds.Data.Count; i++)
                {
                    double v = ds.Data[i] ?? 0;
                    var bucket = v >= 0 ? posSums : negSums;
                    bucket[i] = (bucket.TryGetValue(i, out var s) ? s : 0) + v;
                }
            }
            foreach (var v in posSums.Values) { min = Math.Min(min, v); max = Math.Max(max, v); }
            foreach (var v in negSums.Values) { min = Math.Min(min, v); max = Math.Max(max, v); }
            if (posSums.Count > 0) min = Math.Min(min, 0);
            if (negSums.Count > 0) max = Math.Max(max, 0);

            // Floating bars / point datasets on a stacked axis still contribute their own extent.
            for (int d = 0; d < _data.Datasets.Count; d++)
            {
                var ds = _data.Datasets[d];
                if (ds.YAxisID != axisId || IsHidden(d, ds)) continue;
                if (ds.RangeData is { } ranges)
                    foreach (var r in ranges)
                        if (r is { } rr) { min = Math.Min(min, Math.Min(rr.Low, rr.High)); max = Math.Max(max, Math.Max(rr.Low, rr.High)); }
                if (ds.Points is { } pts)
                    foreach (var p in pts) { min = Math.Min(min, p.Y); max = Math.Max(max, p.Y); }
            }
        }
        else
        {
            for (int d = 0; d < _data.Datasets.Count; d++)
            {
                var ds = _data.Datasets[d];
                if (ds.YAxisID != axisId || IsHidden(d, ds)) continue;
                if (ds.RangeData is { } ranges)
                    foreach (var r in ranges)
                        if (r is { } rr) { min = Math.Min(min, Math.Min(rr.Low, rr.High)); max = Math.Max(max, Math.Max(rr.Low, rr.High)); }
                if (ds.Points is { } pts)
                    foreach (var p in pts) { min = Math.Min(min, p.Y); max = Math.Max(max, p.Y); }
                else
                    foreach (var v in ds.Data) if (v is { } val) { min = Math.Min(min, val); max = Math.Max(max, val); }
            }
        }

        if (double.IsInfinity(min)) { min = 0; max = 1; }
        return (min, max);
    }

    private (double, double) ComputeIndexExtent()
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        for (int d = 0; d < _data.Datasets.Count; d++)
        {
            var ds = _data.Datasets[d];
            if (IsHidden(d, ds)) continue;
            if (ds.Points is { } pts)
                foreach (var p in pts) { min = Math.Min(min, p.X); max = Math.Max(max, p.X); }
        }
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        return (min, max);
    }

    /// <summary>Computes the x-value extent for datasets bound to a specific (secondary) x axis.</summary>
    private (double, double) ComputeXExtent(string axisId)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        for (int d = 0; d < _data.Datasets.Count; d++)
        {
            var ds = _data.Datasets[d];
            if (IsHidden(d, ds) || (string.IsNullOrEmpty(ds.XAxisID) ? "x" : ds.XAxisID) != axisId) continue;
            if (ds.Points is { } pts)
                foreach (var p in pts) { min = Math.Min(min, p.X); max = Math.Max(max, p.X); }
            else
                for (int i = 0; i < ds.Data.Count; i++) { min = Math.Min(min, i); max = Math.Max(max, i); }
        }
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        return (min, max);
    }
}
