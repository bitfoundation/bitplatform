namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private bool IsVertical => _options.IndexAxis == BitChartIndexAxis.X;

    private void RenderCartesian(BitChartScene scene)
    {
        var area = ContentArea();

        // Identify which scale ids are x-axes (datasets' XAxisID plus the default "x").
        var xIds = new HashSet<string>(_data.Datasets.Select(d => string.IsNullOrEmpty(d.XAxisID) ? "x" : d.XAxisID)) { "x" };

        var indexScaleOpts = Scale("x");
        bool indexIsCategory = indexScaleOpts.Type == BitChartScaleType.Category;

        // Value (y) axes used by datasets.
        var leftAxes = new List<BitChartAxisScale>();
        var rightAxes = new List<BitChartAxisScale>();
        // Axes drawn at the other axis' zero line instead of along an edge. They cost no layout space,
        // which is exactly the point: they live inside the plot.
        var centerAxes = new List<BitChartAxisScale>();
        var valueScales = new Dictionary<string, BitChartAxisScale>();

        foreach (var (id, so) in _scales)
        {
            if (xIds.Contains(id) || so.Type == BitChartScaleType.RadialLinear) continue;
            var (mn, mx) = ComputeValueExtent(id);
            var scale = new BitChartAxisScale(so, horizontal: !IsVertical) { Culture = Culture };
            scale.SetDataRange(mn, mx);
            scene.DataRanges[id] = (scale.Min, scale.Max);
            if (so.Type != BitChartScaleType.Category && _state.AxisRanges.TryGetValue(id, out var ov))
            {
                scale.Forced = ov;
                scale.SetDataRange(mn, mx);
            }
            // Provisional pixel range spanning the content box: close enough to the final one that
            // tick-label measurement (and the fit-based tick limit) reserves the right amount of space.
            if (IsVertical) scale.SetPixelRange(area.Bottom, area.Top);
            else scale.SetPixelRange(area.Left, area.Right);
            valueScales[id] = scale;
            if (so.Reverse) scene.ReversedAxes.Add(id);
            if (so.Type != BitChartScaleType.Category) scene.ZoomableAxes.Add(id);
            if (!so.Display) continue;
            switch (PositionOf(so, IsVertical ? BitChartPosition.Left : BitChartPosition.Bottom))
            {
                case BitChartPosition.Right or BitChartPosition.Top: rightAxes.Add(scale); break;
                case BitChartPosition.Center: centerAxes.Add(scale); break;
                default: leftAxes.Add(scale); break;
            }
        }

        // X axes ("x" is the primary index scale; others are secondary, point-bound).
        var xScales = new Dictionary<string, BitChartAxisScale>();
        var bottomXAxes = new List<BitChartAxisScale>();
        var topXAxes = new List<BitChartAxisScale>();
        BitChartAxisScale indexScale = default!;

        foreach (var id in xIds.OrderBy(s => s == "x" ? 0 : 1))
        {
            var so = Scale(id);
            BitChartAxisScale xs;
            if (id == "x" && indexIsCategory)
            {
                xs = new BitChartAxisScale(so, horizontal: IsVertical, categories: _data.Labels) { Culture = Culture };
                xs.SetDataRange(0, Math.Max(0, _data.Labels.Count - 1));
                scene.DataRanges[id] = (xs.Min, xs.Max);
                if (_state.AxisRanges.TryGetValue("x", out var cov))
                {
                    xs.Forced = cov;
                    xs.SetDataRange(0, Math.Max(0, _data.Labels.Count - 1));
                }
            }
            else
            {
                var (mn, mx) = id == "x" ? ComputeIndexExtent() : ComputeXExtent(id);
                xs = new BitChartAxisScale(so, horizontal: IsVertical) { Culture = Culture };
                xs.SetDataRange(mn, mx);
                scene.DataRanges[id] = (xs.Min, xs.Max);
                if (_state.AxisRanges.TryGetValue(id, out var ov))
                {
                    xs.Forced = ov;
                    xs.SetDataRange(mn, mx);
                }
            }
            if (IsVertical) xs.SetPixelRange(area.Left, area.Right);
            else xs.SetPixelRange(area.Top, area.Bottom);
            xScales[id] = xs;
            if (so.Reverse) scene.ReversedAxes.Add(id);
            scene.ZoomableAxes.Add(id);
            if (id == "x") indexScale = xs;
            if (so.Display)
            {
                if (PositionOf(so, BitChartPosition.Bottom) == BitChartPosition.Top) topXAxes.Add(xs);
                else bottomXAxes.Add(xs);
            }
        }

        // ---- Reserve space for axes ----
        // Which side an axis lives on follows the orientation, not the axis' role: with a vertical
        // index axis (horizontal bars) the categories run down the left edge and the values along the
        // bottom, so the reservations swap - a side axis needs label *width*, an edge axis needs
        // label *height*.
        double leftReserve, rightReserve, bottomReserve, topReserve;
        if (IsVertical)
        {
            leftReserve = leftAxes.Sum(ReserveAxisWidth);
            rightReserve = rightAxes.Sum(ReserveAxisWidth);

            // Auto-rotate category labels on the bottom (x) axis when they don't fit.
            if (indexIsCategory && indexScaleOpts.Display && indexScaleOpts.Ticks.Display)
                indexScale.LabelRotation = ComputeIndexLabelRotation(indexScale, area.Width - leftReserve - rightReserve);

            bottomReserve = bottomXAxes.Sum(ReserveAxisHeight);
            topReserve = topXAxes.Sum(ReserveAxisHeight);
        }
        else
        {
            leftReserve = ReserveAxisWidth(indexScale);
            rightReserve = 0;
            bottomReserve = leftAxes.Concat(rightAxes).Sum(ReserveAxisHeight);
            topReserve = 0;
        }

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
        DrawGrid(scene, plot, indexScale, leftAxes, rightAxes, centerAxes);

        // Secondary x axes (display only; stacked outside the plot, no chart-area grid).
        if (IsVertical)
        {
            double belowOffset = ReserveAxisHeight(indexScale);
            foreach (var xs in bottomXAxes)
            {
                if (ReferenceEquals(xs, indexScale)) continue;
                DrawSecondaryXAxis(scene, plot, xs, plot.Bottom + belowOffset, atBottom: true);
                belowOffset += ReserveAxisHeight(xs);
            }
            double aboveOffset = 0;
            foreach (var xs in topXAxes)
            {
                if (ReferenceEquals(xs, indexScale)) continue;
                aboveOffset += ReserveAxisHeight(xs);
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

        BitChartAxisScale ValueScaleFor(BitChartDataset ds)
            => valueScales.TryGetValue(ds.YAxisID, out var vs) ? vs : valueScales.Values.First();

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
            var vScale = ValueScaleFor(ds);
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

        BuildHitBands(scene, plot, indexScale, indexIsCategory, centered);

        foreach (var plugin in _options.Plugins.Custom) plugin.AfterDatasetsDraw(ctx);
    }

    /// <summary>
    /// Builds the invisible per-index hit areas that make hovering work anywhere inside the plot.
    /// Skipped when the interaction is configured to require an intersection with a real element.
    /// </summary>
    private void BuildHitBands(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale,
        bool indexIsCategory, bool centered)
    {
        if (EffectiveIntersect() || scene.Elements.Count == 0) return;

        // Bands map one index to one slice of the plot, which only makes sense when the datasets share
        // that index. Scatter/bubble points are placed by their own x value, so they keep pure
        // nearest-element hovering instead.
        bool indexAligned = _data.Datasets
            .Where((d, i) => !IsHidden(i, d))
            .All(d => EffectiveType(d) is BitChartType.Bar or BitChartType.Line);
        if (!indexAligned) return;

        // One band per distinct index, ordered by where that index actually sits along the axis.
        var centers = new Dictionary<int, double>();
        foreach (var el in scene.Elements)
        {
            double c = IsVertical ? el.CenterX : el.CenterY;
            centers[el.DataIndex] = centers.TryGetValue(el.DataIndex, out var existing) ? (existing + c) / 2 : c;
        }
        if (centers.Count is 0 or > 1000) return;

        var ordered = centers.OrderBy(kv => kv.Value).ToList();
        double lo = IsVertical ? plot.Left : plot.Top;
        double hi = IsVertical ? plot.Right : plot.Bottom;

        for (int k = 0; k < ordered.Count; k++)
        {
            double c = ordered[k].Value;
            double prev = k > 0 ? ordered[k - 1].Value : lo - (c - lo);
            double next = k < ordered.Count - 1 ? ordered[k + 1].Value : hi + (hi - c);
            double start = Math.Max(lo, (prev + c) / 2);
            double end = Math.Min(hi, (c + next) / 2);
            if (end <= start) continue;

            scene.HitBands.Add(IsVertical
                ? new BitChartHitBand { X = start, Y = plot.Top, Width = end - start, Height = plot.Height, DataIndex = ordered[k].Key }
                : new BitChartHitBand { X = plot.Left, Y = start, Width = plot.Width, Height = end - start, DataIndex = ordered[k].Key });
        }
    }

    /// <summary>The interaction mode in effect for hover/tooltip (the tooltip may override the global one).</summary>
    internal BitChartInteractionMode EffectiveMode()
        => _options.Plugins.Tooltip.Mode ?? _options.Interaction.Mode;

    /// <summary>Whether the pointer must intersect an element for it to activate.</summary>
    internal bool EffectiveIntersect()
        => _options.Plugins.Tooltip.Intersect ?? _options.Interaction.Intersect;

    private BitChartType EffectiveType(BitChartDataset ds) => ds.Type ?? _config.Type;
    private bool IsHidden(int i, BitChartDataset ds) => ds.Hidden || _state.IsDatasetHidden(i);

    /// <summary>The height an axis drawn along the top or bottom edge needs for its ticks and title.</summary>
    private double ReserveAxisHeight(BitChartAxisScale scale)
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

    /// <summary>The width an axis drawn along the left or right edge needs for its ticks and title.</summary>
    private double ReserveAxisWidth(BitChartAxisScale scale)
    {
        // Memoized per render, for two reasons: measuring every tick label of every axis again on each
        // of the later calls adds up, and the drawing code must place the axis using exactly the width
        // that was reserved for it - even though the tick set is rebuilt once the plot area is known.
        if (_widthReserve.TryGetValue(scale, out var cached)) return cached;

        var o = scale.Options;
        double w;
        if (!o.Display)
        {
            w = 0;
        }
        else
        {
            double maxLabel = 0;
            if (o.Ticks.Display && !o.Ticks.Mirror)
                foreach (var t in scale.Ticks)
                    maxLabel = Math.Max(maxLabel, EstimateTextWidth(t.Label, o.Ticks.Font.Size));
            w = maxLabel + o.Ticks.Padding;
            if (o.Grid.DrawTicks) w += o.Grid.TickLength;
            if (o.Title.Display) w += o.Title.Font.LineHeightPx + o.Title.Padding.Horizontal;
            w += 2;
        }
        _widthReserve[scale] = w;
        return w;
    }

    private readonly Dictionary<BitChartAxisScale, double> _widthReserve = new();

    // ---- extents ----

    /// <summary>
    /// The stack bucket a dataset belongs to. It has to match how the drawing code groups: bars stack
    /// among bars (DrawBars) and lines among lines (DrawStackedAreas), each within its own Stack id.
    /// </summary>
    private string StackKey(BitChartDataset ds)
        => (EffectiveType(ds) == BitChartType.Bar ? "bar:" : "line:") + (ds.Stack ?? "default");

    private (double, double) ComputeValueExtent(string axisId)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        var scaleOpts = Scale(axisId);

        if (scaleOpts.Stacked && scaleOpts.Stacked100)
            return (0, 100);

        if (scaleOpts.Stacked)
        {
            // Sums are tracked per (stack group, index, sign) so two independent stacks standing side by
            // side do not add up into one oversized range.
            var sums = new Dictionary<(string stack, int index, int sign), double>();
            for (int d = 0; d < _data.Datasets.Count; d++)
            {
                var ds = _data.Datasets[d];
                if (ds.YAxisID != axisId || IsHidden(d, ds)) continue;
                string key = StackKey(ds);
                for (int i = 0; i < ds.Data.Count; i++)
                {
                    if (ds.Data[i] is not { } v) continue;
                    int sign = v >= 0 ? 1 : -1;
                    sums[(key, i, sign)] = sums.GetValueOrDefault((key, i, sign), 0) + v;
                }
            }
            foreach (var v in sums.Values) { min = Math.Min(min, v); max = Math.Max(max, v); }
            if (sums.Count > 0) { min = Math.Min(min, 0); max = Math.Max(max, 0); }

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
                if (ds.Base is { } b) { min = Math.Min(min, b); max = Math.Max(max, b); }
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
