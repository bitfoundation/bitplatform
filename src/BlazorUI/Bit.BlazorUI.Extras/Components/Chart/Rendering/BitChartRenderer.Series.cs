using System.Text;

namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private void DrawBars(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale,
        Dictionary<string, BitChartAxisScale> valueScales, List<(BitChartDataset d, int i)> barItems, bool indexIsCategory)
    {
        if (barItems.Count == 0) return;

        // Build slots: stacked datasets share a slot, others get their own. A dataset with
        // Grouped = false opts out of the layout entirely and spans the whole category band.
        var slotKeys = new List<string>();
        var dsSlot = new Dictionary<int, int>();
        foreach (var (ds, i) in barItems)
        {
            if (!ds.Grouped) { dsSlot[i] = -1; continue; }
            bool stacked = valueScales[ds.YAxisID].Options.Stacked;
            string key = stacked ? $"stack:{ds.Stack ?? "default"}:{ds.YAxisID}" : $"ds:{i}";
            int at = slotKeys.IndexOf(key);
            if (at < 0) { slotKeys.Add(key); at = slotKeys.Count - 1; }
            dsSlot[i] = at;
        }
        int slotCount = Math.Max(1, slotKeys.Count);

        double band = indexScale.BandWidth();
        var first = barItems[0].d;
        double categorySize = band * first.CategoryPercentage;

        // Per-index slot layout. With SkipNull the slots whose datasets have no value at an index are
        // dropped, so the remaining bars widen to fill the category instead of leaving a hole.
        bool skipNull = barItems.Any(t => t.d.SkipNull);
        var allSlots = Enumerable.Range(0, slotKeys.Count).ToList();
        var perIndexSlots = new Dictionary<int, List<int>>();
        List<int> SlotsAt(int di)
        {
            if (!skipNull) return allSlots;
            if (perIndexSlots.TryGetValue(di, out var cached)) return cached;
            var live = new List<int>();
            for (int k = 0; k < slotKeys.Count; k++)
            {
                bool any = barItems.Any(t => dsSlot[t.i] == k && HasValueAt(t.d, di));
                if (any) live.Add(k);
            }
            perIndexSlots[di] = live;
            return live;
        }

        var stackOffset = new Dictionary<(int slot, int di, int sign), double>();

        // Precompute per-(slot,index) totals for 100% stacking.
        var stack100Totals = new Dictionary<(int slot, int di), double>();
        foreach (var (ds, i) in barItems)
        {
            var so = valueScales[ds.YAxisID].Options;
            if (!(so.Stacked && so.Stacked100)) continue;
            int slot = dsSlot[i];
            for (int di = 0; di < ds.Data.Count; di++)
                if (ds.Data[di] is { } v)
                    stack100Totals[(slot, di)] = stack100Totals.GetValueOrDefault((slot, di), 0) + Math.Abs(v);
        }

        // The value-axis baseline every bar grows out of. Taken from the first bar dataset (they share
        // the axis) so the group entry animation always scales out of the axis line - not out of
        // whatever the last drawn bar happened to sit on.
        var firstScale = valueScales[first.YAxisID];
        double axisBaseValue = first.Base ?? Math.Clamp(0, Math.Min(firstScale.Min, firstScale.Max), Math.Max(firstScale.Min, firstScale.Max));
        scene.BarBaseline = firstScale.PixelFor(axisBaseValue);

        foreach (var (ds, i) in barItems)
        {
            var vScale = valueScales[ds.YAxisID];
            bool stacked = vScale.Options.Stacked;
            bool stacked100 = stacked && vScale.Options.Stacked100;
            int slot = dsSlot[i];
            var barType = EffectiveType(ds);

            int count = ds.Count;
            string? patternFill = ds.BackgroundPattern is { } pat ? RegisterPattern(scene, pat) : null;
            double borderWidth = ResolveBorderWidth(ds, BitChartType.Bar, i);

            for (int di = 0; di < count; di++)
            {
                double baseVal, topVal, tooltipVal;
                bool isRange = ds.RangeData is { } rd && di < rd.Count && rd[di].HasValue;

                if (isRange)
                {
                    var (low, high) = ds.RangeData![di]!.Value;
                    baseVal = low; topVal = high; tooltipVal = high;
                }
                else
                {
                    if (di >= ds.Data.Count || ds.Data[di] is not { } v) continue;
                    double value = v;
                    if (stacked100)
                    {
                        double total = stack100Totals.GetValueOrDefault((slot, di), 0);
                        if (total > 0) value = value / total * 100;
                    }
                    tooltipVal = v;
                    int sign = value >= 0 ? 1 : -1;
                    if (stacked)
                    {
                        baseVal = stackOffset.GetValueOrDefault((slot, di, sign), 0);
                        topVal = baseVal + value;
                        stackOffset[(slot, di, sign)] = topVal;
                    }
                    else
                    {
                        baseVal = ds.Base ?? Math.Clamp(0, Math.Min(vScale.Min, vScale.Max), Math.Max(vScale.Min, vScale.Max));
                        topVal = value;
                    }
                }

                // Slot geometry for this index.
                double slotSize, slotCenter;
                double centerAlong = indexIsCategory ? indexScale.PixelForIndex(di, true) : indexScale.PixelFor(di);
                if (slot < 0)
                {
                    slotSize = categorySize;
                    slotCenter = centerAlong;
                }
                else
                {
                    var live = SlotsAt(di);
                    int ordinal = live.IndexOf(slot);
                    int liveCount = Math.Max(1, live.Count);
                    if (ordinal < 0) { ordinal = slot; liveCount = slotCount; }
                    slotSize = categorySize / liveCount;
                    slotCenter = centerAlong - categorySize / 2 + ordinal * slotSize + slotSize / 2;
                }

                double barSize = slotSize * ds.BarPercentage;
                if (ds.BarThickness is { } bt) barSize = bt;
                if (ds.MaxBarThickness is { } mbt) barSize = Math.Min(barSize, mbt);

                string bg = ResolveBackground(ds, i, di, false, tooltipVal);
                string border = ResolveBorder(ds, i, di, false, tooltipVal, fallbackToBackground: true);
                if (patternFill is not null) bg = patternFill;
                double inflate = ds.InflateAmount ?? 0;
                double minLen = ds.MinBarLength ?? 1;
                BitChartSvgRect rect;
                double cx, cy;

                // Which way the bar grows is a question about pixels, not about the sign of the value: a
                // reversed axis puts a positive bar below its baseline. Everything downstream - the
                // skipped edge, the rounded corners, the data label - follows this, not the raw sign.
                int signFinal;

                if (IsVertical)
                {
                    double yBase = vScale.PixelFor(baseVal);
                    double yTop = vScale.PixelFor(topVal);
                    signFinal = yTop <= yBase ? 1 : -1;
                    // A minimum length still grows away from the baseline rather than straddling it.
                    double height = Math.Max(minLen, Math.Abs(yBase - yTop));
                    double y = signFinal >= 0 ? yBase - height : yBase;
                    rect = new BitChartSvgRect
                    {
                        X = slotCenter - barSize / 2 - inflate,
                        Y = y - inflate,
                        Width = barSize + inflate * 2,
                        Height = height + inflate * 2,
                        Fill = bg
                    };
                    cx = slotCenter; cy = signFinal >= 0 ? rect.Y : rect.Y + rect.Height;
                }
                else
                {
                    double xBase = vScale.PixelFor(baseVal);
                    double xTop = vScale.PixelFor(topVal);
                    signFinal = xTop >= xBase ? 1 : -1;
                    double width = Math.Max(minLen, Math.Abs(xBase - xTop));
                    double x = signFinal >= 0 ? xBase : xBase - width;
                    rect = new BitChartSvgRect
                    {
                        X = x - inflate,
                        Y = slotCenter - barSize / 2 - inflate,
                        Width = width + inflate * 2,
                        Height = barSize + inflate * 2,
                        Fill = bg
                    };
                    cx = signFinal >= 0 ? rect.X + rect.Width : rect.X; cy = slotCenter;
                }

                var skip = ResolveSkip(isRange ? BitChartBorderSkipped.None : ds.BorderSkipped, IsVertical, signFinal);

                // Effective corner radii. Chart.js only rounds the corners that are not adjacent to the
                // skipped (baseline) edge, so a bar with a uniform BorderRadius rounds its tip and keeps
                // a flat foot on the axis. BorderSkipped.None opts back into rounding all four corners.
                BitChartBorderRadiusCorners? roundedCorners = ds.BorderRadiusCorners
                    ?? (ds.BorderRadius > 0 ? CornersForSkip(ds.BorderRadius, skip) : null);
                bool rounded = roundedCorners is { } rc0 &&
                    (rc0.TopLeft > 0 || rc0.TopRight > 0 || rc0.BottomRight > 0 || rc0.BottomLeft > 0);

                BitChartSvgNode shapeNode = rect;
                if (rounded)
                    shapeNode = new BitChartSvgPath
                    {
                        D = RoundedRectPath(rect.X, rect.Y, rect.Width, rect.Height, roundedCorners!.Value),
                        Fill = bg
                    };

                // Border honoring borderSkipped. Rounded bars get a matching rounded border path so the
                // stroke follows the corner radius instead of cutting square corners.
                BitChartSvgNode? borderNode = null;
                if (borderWidth > 0)
                {
                    if (rounded)
                    {
                        borderNode = new BitChartSvgPath
                        {
                            D = RoundedBarBorderPath(rect.X, rect.Y, rect.Width, rect.Height, roundedCorners!.Value, skip),
                            Fill = "none", Stroke = border, StrokeWidth = borderWidth
                        };
                    }
                    else if (skip == BitChartBorderSkipped.None)
                    {
                        rect.Stroke = border;
                        rect.StrokeWidth = borderWidth;
                    }
                    else
                    {
                        // Drawn as part of the element so it animates together with the fill.
                        borderNode = new BitChartSvgPath
                        {
                            D = BarBorderPath(rect, skip), Fill = "none", Stroke = border, StrokeWidth = borderWidth
                        };
                    }
                }

                // Hover appearance, precomputed so hovering never triggers a re-layout.
                string hoverBg = ds.HoverBackgroundColor
                    ?? (ds.BackgroundColorFn is not null ? ResolveBackground(ds, i, di, false, tooltipVal, active: true) : BitChartColorUtil.Adjust(bg, -0.08));
                if (patternFill is not null) hoverBg = bg;
                string hoverBorder = ds.HoverBorderColor ?? border;
                double hoverBorderWidth = ds.HoverBorderWidth ?? borderWidth;
                BitChartSvgNode hoverNode = rounded
                    ? new BitChartSvgPath
                    {
                        D = RoundedRectPath(rect.X, rect.Y, rect.Width, rect.Height, roundedCorners!.Value),
                        Fill = hoverBg, Stroke = hoverBorderWidth > 0 ? hoverBorder : null, StrokeWidth = hoverBorderWidth
                    }
                    : new BitChartSvgRect
                    {
                        X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height,
                        Fill = hoverBg, Stroke = hoverBorderWidth > 0 ? hoverBorder : null, StrokeWidth = hoverBorderWidth
                    };

                string text = isRange
                    ? $"{(ds.Label is null ? "" : ds.Label + ": ")}[{FormatNumber(baseVal, "0.##")}, {FormatNumber(topVal, "0.##")}]"
                    : BuildItemText(ds, i, di, tooltipVal, bg) + ErrorSuffix(ds, di);

                scene.Elements.Add(new BitChartDataElement
                {
                    Shape = shapeNode,
                    BorderShape = borderNode,
                    HoverShape = hoverNode,
                    DatasetIndex = i,
                    DataIndex = di,
                    CenterX = IsVertical ? cx : (rect.X + rect.Width / 2),
                    CenterY = IsVertical ? (rect.Y + rect.Height / 2) : cy,
                    Value = tooltipVal,
                    SeriesLabel = ds.Label,
                    Tooltip = new BitChartTooltipInfo
                    {
                        Title = di < _data.Labels.Count ? _data.Labels[di] : null,
                        // Anchored at the bar's tip, so a negative bar points at its own end and the
                        // tooltip flips below it rather than hovering over the baseline.
                        AnchorX = cx,
                        AnchorY = cy,
                        Items = { new BitChartTooltipItem { Color = bg, Text = text } }
                    }
                });

                if (!isRange)
                {
                    // Centered on the bar's own tip, so a grouped bar keeps its whisker over itself.
                    // A percentage stack rescales every value, which would leave the interval - still in
                    // the original units - pointing at the wrong place, so it is left out there.
                    if (!stacked100) AddErrorBar(scene, ds, di, topVal, slotCenter, vScale);
                    AddBarDataLabel(scene, tooltipVal, rect, signFinal, i, di);
                }
            }
        }
    }

    /// <summary>The error bar attached to one index, if the dataset has one there.</summary>
    private static BitChartErrorBar? ErrorAt(BitChartDataset ds, int dataIndex)
        => ds.ErrorData is { } errors && dataIndex >= 0 && dataIndex < errors.Count ? errors[dataIndex] : null;

    /// <summary>
    /// The interval a tooltip names after the value, so the uncertainty is readable and not only
    /// visible. A symmetric interval reads as a single plus-minus; an asymmetric one names both arms.
    /// </summary>
    private string ErrorSuffix(BitChartDataset ds, int dataIndex)
    {
        if (ErrorAt(ds, dataIndex) is not { } e) return "";
        double minus = Math.Abs(e.Minus), plus = Math.Abs(e.Plus);
        if (minus <= 0 && plus <= 0) return "";
        return e.IsSymmetric
            ? $" ±{FormatNumber(plus, "0.##")}"
            : $" +{FormatNumber(plus, "0.##")}/-{FormatNumber(minus, "0.##")}";
    }

    /// <summary>
    /// Draws the whisker for one value: a line spanning the interval along the value axis, with a cap at
    /// each end. It goes in the foreground so it stays legible over the bar or point it belongs to.
    /// </summary>
    private void AddErrorBar(BitChartScene scene, BitChartDataset ds, int dataIndex, double value,
        double centerAlongIndexAxis, BitChartAxisScale valueScale)
    {
        if (ErrorAt(ds, dataIndex) is not { } e) return;
        double minus = Math.Abs(e.Minus), plus = Math.Abs(e.Plus);
        if (minus <= 0 && plus <= 0) return;

        double low = valueScale.PixelFor(value - minus);
        double high = valueScale.PixelFor(value + plus);
        string color = ds.ErrorBarColor ?? "var(--bit-clr-fg-pri, #1A1A1A)";
        double width = ds.ErrorBarWidth;
        double cap = Math.Max(0, ds.ErrorBarCapWidth) / 2;
        double c = centerAlongIndexAxis;

        if (IsVertical)
        {
            scene.Foreground.Add(new BitChartSvgLine { X1 = c, Y1 = low, X2 = c, Y2 = high, Stroke = color, StrokeWidth = width });
            if (cap <= 0) return;
            scene.Foreground.Add(new BitChartSvgLine { X1 = c - cap, Y1 = low, X2 = c + cap, Y2 = low, Stroke = color, StrokeWidth = width });
            scene.Foreground.Add(new BitChartSvgLine { X1 = c - cap, Y1 = high, X2 = c + cap, Y2 = high, Stroke = color, StrokeWidth = width });
        }
        else
        {
            scene.Foreground.Add(new BitChartSvgLine { X1 = low, Y1 = c, X2 = high, Y2 = c, Stroke = color, StrokeWidth = width });
            if (cap <= 0) return;
            scene.Foreground.Add(new BitChartSvgLine { X1 = low, Y1 = c - cap, X2 = low, Y2 = c + cap, Stroke = color, StrokeWidth = width });
            scene.Foreground.Add(new BitChartSvgLine { X1 = high, Y1 = c - cap, X2 = high, Y2 = c + cap, Stroke = color, StrokeWidth = width });
        }
    }

    private static bool HasValueAt(BitChartDataset ds, int di)
    {
        // A dataset carrying ranges still falls back to its plain values where a range is missing,
        // which is exactly how DrawBars picks the value it draws.
        if (ds.RangeData is { } rd && di < rd.Count && rd[di].HasValue) return true;
        return di < ds.Data.Count && ds.Data[di].HasValue;
    }

    /// <summary>Rounds only the corners that are not adjacent to the skipped edge (Chart.js semantics).</summary>
    private static BitChartBorderRadiusCorners CornersForSkip(double r, BitChartBorderSkipped skip) => skip switch
    {
        BitChartBorderSkipped.Bottom => new(r, r, 0, 0),
        BitChartBorderSkipped.Top => new(0, 0, r, r),
        BitChartBorderSkipped.Left => new(0, r, r, 0),
        BitChartBorderSkipped.Right => new(r, 0, 0, r),
        _ => new(r, r, r, r)
    };

    /// <summary>Resolves Start/End border-skip to a concrete edge based on orientation and sign.</summary>
    private static BitChartBorderSkipped ResolveSkip(BitChartBorderSkipped s, bool vertical, int sign) => s switch
    {
        BitChartBorderSkipped.Start => vertical ? (sign >= 0 ? BitChartBorderSkipped.Bottom : BitChartBorderSkipped.Top)
                                        : (sign >= 0 ? BitChartBorderSkipped.Left : BitChartBorderSkipped.Right),
        BitChartBorderSkipped.End => vertical ? (sign >= 0 ? BitChartBorderSkipped.Top : BitChartBorderSkipped.Bottom)
                                      : (sign >= 0 ? BitChartBorderSkipped.Right : BitChartBorderSkipped.Left),
        _ => s
    };

    /// <summary>Builds an open border path for a bar, omitting the skipped edge.</summary>
    private static string BarBorderPath(BitChartSvgRect r, BitChartBorderSkipped skip)
    {
        double x1 = r.X, y1 = r.Y, x2 = r.X + r.Width, y2 = r.Y + r.Height;
        var sb = new StringBuilder();
        void Edge(double ax, double ay, double bx, double by)
            => sb.Append($"M {BitChartSvg.N(ax)} {BitChartSvg.N(ay)} L {BitChartSvg.N(bx)} {BitChartSvg.N(by)} ");
        if (skip != BitChartBorderSkipped.Top) Edge(x1, y1, x2, y1);
        if (skip != BitChartBorderSkipped.Right) Edge(x2, y1, x2, y2);
        if (skip != BitChartBorderSkipped.Bottom) Edge(x2, y2, x1, y2);
        if (skip != BitChartBorderSkipped.Left) Edge(x1, y2, x1, y1);
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Builds a border path for a rounded bar that follows the corner radii, omitting the skipped edge.
    /// The outline is traced clockwise; the skipped straight edge is dropped so the stroke matches the
    /// rounded fill exactly (instead of cutting square corners).
    /// </summary>
    private static string RoundedBarBorderPath(double x, double y, double w, double h,
        BitChartBorderRadiusCorners c, BitChartBorderSkipped skip)
    {
        double max = Math.Min(w, h) / 2;
        double tl = Math.Clamp(c.TopLeft, 0, max);
        double tr = Math.Clamp(c.TopRight, 0, max);
        double br = Math.Clamp(c.BottomRight, 0, max);
        double bl = Math.Clamp(c.BottomLeft, 0, max);

        // Vertices around the rounded rectangle, clockwise starting at the end of the top-left corner.
        var pts = new (double X, double Y)[]
        {
            (x + tl, y),         // P0  start of top edge
            (x + w - tr, y),     // P1  end of top edge
            (x + w, y + tr),     // P2  end of top-right corner
            (x + w, y + h - br), // P3  end of right edge
            (x + w - br, y + h), // P4  end of bottom-right corner
            (x + bl, y + h),     // P5  end of bottom edge
            (x, y + h - bl),     // P6  end of bottom-left corner
            (x, y + tl),         // P7  end of left edge
        };
        // Segment i draws from pts[i] to pts[(i+1)%8]. Odd segments are corner arcs.
        double[] arcR = { 0, tr, 0, br, 0, bl, 0, tl };
        bool[] isArc = { false, true, false, true, false, true, false, true };

        int skipSeg = skip switch
        {
            BitChartBorderSkipped.Top => 0,
            BitChartBorderSkipped.Right => 2,
            BitChartBorderSkipped.Bottom => 4,
            BitChartBorderSkipped.Left => 6,
            _ => -1
        };

        string Cmd(int seg)
        {
            var end = pts[(seg + 1) % 8];
            return isArc[seg] && arcR[seg] > 0
                ? $"A {BitChartSvg.N(arcR[seg])} {BitChartSvg.N(arcR[seg])} 0 0 1 {BitChartSvg.N(end.X)} {BitChartSvg.N(end.Y)} "
                : $"L {BitChartSvg.N(end.X)} {BitChartSvg.N(end.Y)} ";
        }

        var sb = new StringBuilder();
        if (skipSeg < 0)
        {
            // No skipped edge: closed rounded outline.
            sb.Append($"M {BitChartSvg.N(pts[0].X)} {BitChartSvg.N(pts[0].Y)} ");
            for (int i = 0; i < 8; i++) sb.Append(Cmd(i));
            sb.Append('Z');
            return sb.ToString().Trim();
        }

        // Open path: start just after the skipped edge and walk the remaining seven segments.
        int start = (skipSeg + 1) % 8;
        sb.Append($"M {BitChartSvg.N(pts[start].X)} {BitChartSvg.N(pts[start].Y)} ");
        for (int k = 0; k < 7; k++) sb.Append(Cmd((start + k) % 8));
        return sb.ToString().Trim();
    }

    private void DrawLine(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale, BitChartAxisScale vScale,
        BitChartDataset ds, int dsIndex, bool indexIsCategory, bool centered)
    {
        var pts = new List<(double x, double y, int di, double v)>();

        if (ds.Points is { } points)
        {
            // Line over a linear/time x axis driven by explicit (x, y) points.
            var ordered = points.Select((p, di) => (p, di)).OrderBy(t => t.p.X).ToList();
            foreach (var (p, di) in ordered)
                pts.Add((indexScale.PixelFor(p.X), vScale.PixelFor(p.Y), di, p.Y));
            FlushLine(scene, plot, vScale, ds, dsIndex, pts, indexScale, indexIsCategory, centered);
            return;
        }

        // A single fill paint is registered per dataset so a series broken by nulls does not add a
        // duplicate gradient/pattern definition for every segment.
        string? fillPaint = null;
        for (int di = 0; di < ds.Data.Count; di++)
        {
            if (ds.Data[di] is not { } v)
            {
                if (!ds.SpanGaps)
                {
                    fillPaint = FlushLine(scene, plot, vScale, ds, dsIndex, pts, indexScale, indexIsCategory, centered, fillPaint);
                    pts.Clear();
                }
                continue;
            }
            double x = indexIsCategory ? indexScale.PixelForIndex(di, centered) : indexScale.PixelFor(di);
            double y = vScale.PixelFor(v);
            pts.Add((x, y, di, v));
        }
        FlushLine(scene, plot, vScale, ds, dsIndex, pts, indexScale, indexIsCategory, centered, fillPaint);
    }

    private string? FlushLine(BitChartScene scene, BitChartArea plot, BitChartAxisScale vScale, BitChartDataset ds, int dsIndex,
        List<(double x, double y, int di, double v)> pts,
        BitChartAxisScale? indexScale = null, bool indexIsCategory = false, bool centered = false, string? fillPaint = null)
    {
        if (pts.Count == 0) return fillPaint;

        var dec = _options.Plugins.Decimation;
        if (dec.Enabled && pts.Count > dec.Threshold && dec.Samples >= 2 && dec.Samples < pts.Count)
            pts = BitChartDecimation.Lttb(pts, dec.Samples);

        string border = ResolveBorder(ds, dsIndex, 0, false);
        double lineWidth = ResolveBorderWidth(ds, BitChartType.Line, dsIndex);
        double tension = ResolveTension(ds);
        var xy = pts.Select(p => (p.x, p.y)).ToList();
        string d = BuildPath(xy, tension, ds.Stepped, ds.CubicInterpolationMode);

        bool progressive = _options.Animation.Animate && _options.Animation.Progressive;
        if (progressive) scene.ProgressiveDraw = true;

        if (ds.Fill != BitChartFillMode.None && ds.ShowLine)
        {
            fillPaint ??= ResolveFill(scene, ds, border);
            string? fillD = null;

            // Fill to another dataset's line (range area).
            if (ds.Fill == BitChartFillMode.Dataset && ds.FillTargetIndex is { } ti
                && ti >= 0 && ti < _data.Datasets.Count && indexScale is not null)
            {
                var target = ComputeLinePoints(_data.Datasets[ti], indexScale, vScale, indexIsCategory, centered);
                if (target.Count > 0)
                    fillD = AreaBetween(xy, tension, ds.Stepped, target.Select(p => (p.x, p.y)).ToList());
            }

            if (fillD is null)
            {
                double baseVal = ds.Fill switch
                {
                    BitChartFillMode.Start => Math.Min(vScale.Min, vScale.Max),
                    BitChartFillMode.End => Math.Max(vScale.Min, vScale.Max),
                    BitChartFillMode.Value => ds.FillValue ?? 0,
                    _ => Math.Clamp(0, Math.Min(vScale.Min, vScale.Max), Math.Max(vScale.Min, vScale.Max))
                };
                double baseY = vScale.PixelFor(baseVal);
                fillD = d + $" L {BitChartSvg.N(xy[^1].x)} {BitChartSvg.N(baseY)} L {BitChartSvg.N(xy[0].x)} {BitChartSvg.N(baseY)} Z";
            }

            scene.Series.Add(new BitChartSvgPath { D = fillD, Fill = fillPaint, Stroke = null, AnimateFade = progressive });
        }

        if (ds.ShowLine)
        {
            if (ds.Segment is { } seg)
            {
                // Draw each consecutive segment with its own resolved style.
                for (int k = 0; k < pts.Count - 1; k++)
                {
                    var a = pts[k];
                    var b = pts[k + 1];
                    var sctx = new BitChartSegmentContext(a.di, b.di, a.v, b.v);
                    string color = seg.BorderColor?.Invoke(sctx) ?? border;
                    double width = seg.BorderWidth?.Invoke(sctx) ?? lineWidth;
                    var dash = seg.BorderDash?.Invoke(sctx);
                    scene.Series.Add(new BitChartSvgPath
                    {
                        D = $"M {BitChartSvg.N(a.x)} {BitChartSvg.N(a.y)} L {BitChartSvg.N(b.x)} {BitChartSvg.N(b.y)}",
                        Fill = "none", Stroke = color, StrokeWidth = width,
                        Dash = dash is null ? "" : BitChartSvg.Dash(dash),
                        DashOffset = ds.BorderDashOffset,
                        LineCap = ds.BorderCapStyle, LineJoin = ds.BorderJoinStyle,
                        AnimateFade = progressive
                    });
                }
            }
            else
            {
                bool dashed = ds.BorderDash is { Count: > 0 };
                scene.Series.Add(new BitChartSvgPath
                {
                    D = d, Fill = "none", Stroke = border, StrokeWidth = lineWidth,
                    Dash = BitChartSvg.Dash(ds.BorderDash), DashOffset = ds.BorderDashOffset,
                    LineCap = ds.BorderCapStyle, LineJoin = ds.BorderJoinStyle,
                    // Draw-on reveals the stroke left to right; dashed strokes can't (dasharray is in use), so they fade.
                    AnimateDraw = progressive && !dashed,
                    AnimateFade = progressive && dashed
                });
            }
        }

        if (ds.PointStyle != BitChartPointStyle.None)
            foreach (var p in pts)
                AddPoint(scene, ds, dsIndex, p.di, p.x, p.y, p.v, ResolvePointRadius(ds), border, valueScale: vScale);

        return fillPaint;
    }

    /// <summary>
    /// Resolves an area fill paint. A pattern or gradient wins; then the dataset's explicit
    /// <see cref="BitChartDataset.FillColor"/>, then its <see cref="BitChartDataset.BackgroundColor"/>
    /// (which is what Chart.js paints an area with), and finally a translucent tint of the line color.
    /// </summary>
    private string ResolveFill(BitChartScene scene, BitChartDataset ds, string border)
    {
        if (ds.BackgroundPattern is { } pat) return RegisterPattern(scene, pat);
        if (ds.FillGradient is { Stops.Count: > 0 } g) return RegisterGradient(scene, g);
        if (!string.IsNullOrEmpty(ds.FillColor)) return ds.FillColor!;
        if (!string.IsNullOrEmpty(ds.BackgroundColor)) return ds.BackgroundColor!;
        return BitChartColorUtil.WithAlpha(border, 0.2);
    }

    /// <summary>Computes the pixel polyline for a dataset's line (nulls skipped).</summary>
    private List<(double x, double y, int di, double v)> ComputeLinePoints(
        BitChartDataset ds, BitChartAxisScale indexScale, BitChartAxisScale vScale, bool indexIsCategory, bool centered)
    {
        var pts = new List<(double x, double y, int di, double v)>();
        if (ds.Points is { } points)
        {
            foreach (var (p, di) in points.Select((p, i) => (p, i)).OrderBy(t => t.p.X))
                pts.Add((indexScale.PixelFor(p.X), vScale.PixelFor(p.Y), di, p.Y));
            return pts;
        }
        for (int di = 0; di < ds.Data.Count; di++)
        {
            if (ds.Data[di] is not { } v) continue;
            double x = indexIsCategory ? indexScale.PixelForIndex(di, centered) : indexScale.PixelFor(di);
            pts.Add((x, vScale.PixelFor(v), di, v));
        }
        return pts;
    }

    /// <summary>Builds a closed area path between an upper and lower polyline.</summary>
    private static string AreaBetween(List<(double x, double y)> top, double tension, BitChartSteppedLine stepped,
        List<(double x, double y)> bottom)
    {
        if (bottom.Count == 0) return BuildPath(top, tension, stepped);
        var sb = new StringBuilder(BuildPath(top, tension, stepped));
        var rev = new List<(double x, double y)>(bottom);
        rev.Reverse();
        sb.Append(' ').Append('L').Append(' ').Append(BitChartSvg.N(rev[0].x)).Append(' ').Append(BitChartSvg.N(rev[0].y));
        var tail = BuildPath(rev, tension, stepped);
        // Replace leading "M" of the tail with "L" so it connects.
        if (tail.StartsWith('M')) tail = "L" + tail[1..];
        sb.Append(' ').Append(tail).Append(" Z");
        return sb.ToString();
    }

    /// <summary>Draws stacked line areas (cumulative) for datasets on a stacked value axis.</summary>
    private void DrawStackedAreas(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale,
        Dictionary<string, BitChartAxisScale> valueScales, List<(BitChartDataset d, int i)> items,
        bool indexIsCategory, bool centered)
    {
        // Group by (axis, stack); within each group accumulate per data index.
        foreach (var group in items.GroupBy(t => (t.d.YAxisID, t.d.Stack ?? "default")))
        {
            var vScale = valueScales[group.Key.YAxisID];
            bool stacked100 = vScale.Options.Stacked100;
            var cumulative = new Dictionary<int, double>();

            // 100% stacking normalizes every index against the group's absolute total.
            var totals = new Dictionary<int, double>();
            if (stacked100)
                foreach (var (ds, _) in group)
                    for (int di = 0; di < ds.Data.Count; di++)
                        if (ds.Data[di] is { } v)
                            totals[di] = totals.GetValueOrDefault(di, 0) + Math.Abs(v);

            foreach (var (ds, i) in group)
            {
                var topPts = new List<(double x, double y, int di, double v, double top)>();
                var basePts = new List<(double x, double y)>();
                for (int di = 0; di < ds.Data.Count; di++)
                {
                    if (ds.Data[di] is not { } raw) continue;
                    double v = raw;
                    if (stacked100)
                    {
                        double total = totals.GetValueOrDefault(di, 0);
                        if (total > 0) v = v / total * 100;
                    }
                    double baseVal = cumulative.GetValueOrDefault(di, 0);
                    double topVal = baseVal + v;
                    cumulative[di] = topVal;
                    double x = indexIsCategory ? indexScale.PixelForIndex(di, centered) : indexScale.PixelFor(di);
                    topPts.Add((x, vScale.PixelFor(topVal), di, raw, topVal));
                    basePts.Add((x, vScale.PixelFor(baseVal)));
                }
                if (topPts.Count == 0) continue;

                string border = ResolveBorder(ds, i, 0, false);
                double lineWidth = ResolveBorderWidth(ds, BitChartType.Line, i);
                double tension = ResolveTension(ds);
                var topXy = topPts.Select(p => (p.x, p.y)).ToList();

                bool progressive = _options.Animation.Animate && _options.Animation.Progressive;
                if (progressive) scene.ProgressiveDraw = true;
                bool dashed = ds.BorderDash is { Count: > 0 };

                if (ds.Fill != BitChartFillMode.None)
                {
                    string fillD = AreaBetween(topXy, tension, ds.Stepped, basePts);
                    scene.Series.Add(new BitChartSvgPath { D = fillD, Fill = ResolveFill(scene, ds, border), Stroke = null, AnimateFade = progressive });
                }

                scene.Series.Add(new BitChartSvgPath
                {
                    D = BuildPath(topXy, tension, ds.Stepped, ds.CubicInterpolationMode), Fill = "none", Stroke = border,
                    StrokeWidth = lineWidth,
                    Dash = BitChartSvg.Dash(ds.BorderDash), DashOffset = ds.BorderDashOffset,
                    LineCap = ds.BorderCapStyle, LineJoin = ds.BorderJoinStyle,
                    AnimateDraw = progressive && !dashed,
                    AnimateFade = progressive && dashed
                });

                if (ds.PointStyle != BitChartPointStyle.None)
                    foreach (var p in topPts)
                        // The marker sits at the cumulative top, so the whisker is centered there too
                        // rather than at the raw value it is drawn from. A percentage stack rescales
                        // every value, which would leave the interval - still in the original units -
                        // pointing at the wrong place, so it is left out there.
                        AddPoint(scene, ds, i, p.di, p.x, p.y, p.v, ResolvePointRadius(ds), border,
                            valueScale: stacked100 ? null : vScale, errorValue: p.top);
            }
        }
    }

    private void DrawScatter(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale, BitChartAxisScale vScale,
        BitChartDataset ds, int dsIndex, bool bubble)
    {
        if (ds.Points is not { } points) return;
        string border = ResolveBorder(ds, dsIndex, 0, false);
        for (int di = 0; di < points.Count; di++)
        {
            var p = points[di];
            double x = indexScale.PixelFor(p.X);
            double y = vScale.PixelFor(p.Y);
            double r = bubble ? (p.R ?? 5) : Math.Max(4, ResolvePointRadius(ds));
            AddPoint(scene, ds, dsIndex, di, x, y, p.Y, r, border, p.X, vScale);
        }
    }

    private void AddPoint(BitChartScene scene, BitChartDataset ds, int dsIndex, int di,
        double x, double y, double value, double radius, string border, double? xValue = null,
        BitChartAxisScale? valueScale = null, double? errorValue = null)
    {
        var ctx = Ctx(ds, dsIndex, di, value);
        double r = ds.PointRadiusFn?.Invoke(ctx) ?? radius;
        var style = ds.PointStyleFn?.Invoke(ctx) ?? ds.PointStyle;
        string fill = ds.PointBackgroundColorFn?.Invoke(ctx) ?? ds.PointBackgroundColor ?? ResolveBackground(ds, dsIndex, di, false, value);
        string stroke = ds.PointBorderColorFn?.Invoke(ctx) ?? ds.PointBorderColor ?? border;
        double bw = ds.PointBorderWidth;

        // A marker that is hidden or has no radius still needs something to hover: an invisible hit disc
        // keeps the data point reachable (this is what Chart.js's pointHitRadius does).
        BitChartSvgNode? shape = r > 0 ? BitChartPointShapes.Build(style, x, y, r, fill, stroke, bw, ds.PointRotation) : null;
        double hitR = Math.Max(ds.HitRadius, 4);
        shape ??= new BitChartSvgCircle { Cx = x, Cy = y, R = hitR, Fill = "transparent" };

        // The hover appearance is precomputed with Active = true so scriptable options can react to it.
        var hctx = Ctx(ds, dsIndex, di, value, active: true);
        double hr = Math.Max(ds.PointRadiusFn?.Invoke(hctx) ?? r, ds.PointHoverRadius);
        string hFill = ds.PointHoverBackgroundColor ?? ds.PointBackgroundColorFn?.Invoke(hctx) ?? fill;
        string hStroke = ds.PointHoverBorderColor ?? ds.PointBorderColorFn?.Invoke(hctx) ?? stroke;
        double hbw = ds.PointHoverBorderWidth ?? Math.Max(bw, 2);
        var hoverShape = BitChartPointShapes.Build(style == BitChartPointStyle.None ? BitChartPointStyle.Circle : style,
            x, y, Math.Max(hr, 3), hFill, hStroke, hbw, ds.PointRotation);

        string text = (xValue is { } xv
            ? $"({FormatNumber(xv, "0.##")}, {FormatNumber(value, "0.##")})"
            : BuildItemText(ds, dsIndex, di, value, fill)) + ErrorSuffix(ds, di);

        // A whisker needs the value axis to convert its interval, so it is only drawn where the caller
        // could hand one over - which is every cartesian call site, but not the radar's.
        if (valueScale is not null)
            AddErrorBar(scene, ds, di, errorValue ?? value, IsVertical ? x : y, valueScale);

        scene.Elements.Add(new BitChartDataElement
        {
            Shape = shape,
            HoverShape = hoverShape,
            DatasetIndex = dsIndex,
            DataIndex = di,
            CenterX = x,
            CenterY = y,
            Value = value,
            SeriesLabel = ds.Label,
            Tooltip = new BitChartTooltipInfo
            {
                Title = xValue is null && di < _data.Labels.Count ? _data.Labels[di] : ds.Label,
                AnchorX = x,
                AnchorY = y,
                Items = { new BitChartTooltipItem { Color = fill, Text = text, PointStyle = style } }
            }
        });

        if (_options.Plugins.DataLabels is { Display: true, ShowOnPoints: true })
            AddPointDataLabel(scene, value, x, y, r, dsIndex, di);
    }

    /// <summary>Places a bar's data label from the anchor/align/offset options.</summary>
    private void AddBarDataLabel(BitChartScene scene, double value, BitChartSvgRect rect, int sign, int dsIndex, int dataIndex)
    {
        var dl = _options.Plugins.DataLabels;
        if (!dl.Display) return;

        // Outward is the direction away from the baseline: up for positive vertical bars, right for
        // positive horizontal ones.
        double x, y;
        if (IsVertical)
        {
            double tip = sign >= 0 ? rect.Y : rect.Y + rect.Height;
            double baseline = sign >= 0 ? rect.Y + rect.Height : rect.Y;
            double outward = sign >= 0 ? -1 : 1;
            y = dl.Anchor switch
            {
                BitChartAlign.Start => baseline,
                BitChartAlign.Center => rect.Y + rect.Height / 2,
                _ => tip
            };
            y += AlignShift(dl, outward);
            x = rect.X + rect.Width / 2;
        }
        else
        {
            double tip = sign >= 0 ? rect.X + rect.Width : rect.X;
            double baseline = sign >= 0 ? rect.X : rect.X + rect.Width;
            double outward = sign >= 0 ? 1 : -1;
            x = dl.Anchor switch
            {
                BitChartAlign.Start => baseline,
                BitChartAlign.Center => rect.X + rect.Width / 2,
                _ => tip
            };
            x += AlignShift(dl, outward);
            y = rect.Y + rect.Height / 2;
        }
        AddDataLabel(scene, value, x, y, dsIndex, dataIndex);
    }

    /// <summary>Places a point's data label from the anchor/align/offset options (default: above the marker).</summary>
    private void AddPointDataLabel(BitChartScene scene, double value, double x, double y, double radius, int dsIndex, int dataIndex)
    {
        var dl = _options.Plugins.DataLabels;
        double anchorY = dl.Anchor switch
        {
            BitChartAlign.Start => y + radius,
            BitChartAlign.Center => y,
            _ => y - radius
        };
        double outward = dl.Anchor == BitChartAlign.Start ? 1 : -1;
        AddDataLabel(scene, value, x, anchorY + AlignShift(dl, outward), dsIndex, dataIndex);
    }

    /// <summary>How far (and in which direction) the label sits from its anchor.</summary>
    private static double AlignShift(BitChartDataLabelOptions dl, double outward)
    {
        double dist = dl.Offset + dl.Font.Size * 0.5 + dl.Padding;
        return dl.Align switch
        {
            BitChartAlign.End => outward * dist,
            BitChartAlign.Start => -outward * dist,
            _ => 0
        };
    }

    private void AddDataLabel(BitChartScene scene, double value, double x, double y, int dsIndex = 0, int dataIndex = 0)
    {
        var dl = _options.Plugins.DataLabels;
        if (!dl.Display) return;
        if (dl.DisplayFn is { } show && !show(value, dsIndex, dataIndex)) return;

        string text = dl.FormatterCtx?.Invoke(value, dsIndex, dataIndex)
            ?? dl.Formatter?.Invoke(value)
            ?? FormatNumber(value, "0.##");

        // Kept inside the content box: a label pushed past the tip of a bar at the top of the axis
        // would otherwise be clipped by the edge of the chart.
        double halfW = BitChartTextMeasure.Width(text, dl.Font.Size, dl.Font.Weight) / 2 + dl.Padding;
        double halfH = dl.Font.Size / 2 + dl.Padding;
        var box = ContentArea();
        if (box.Width > halfW * 2) x = Math.Clamp(x, box.Left + halfW, box.Right - halfW);
        if (box.Height > halfH * 2) y = Math.Clamp(y, box.Top + halfH, box.Bottom - halfH);

        if (dl.BackgroundColor is { } bgc)
        {
            scene.Foreground.Add(new BitChartSvgRect
            {
                X = x - halfW, Y = y - halfH, Width = halfW * 2, Height = halfH * 2, Rx = dl.BorderRadius, Fill = bgc
            });
        }

        scene.Foreground.Add(new BitChartSvgText
        {
            X = x, Y = y,
            Text = text,
            Fill = dl.Color, FontFamily = dl.Font.Family, FontSize = dl.Font.Size, FontWeight = dl.Font.Weight,
            Anchor = "middle", Baseline = "central", Rotation = dl.Rotation
        });
    }

    /// <summary>Builds an SVG path through points, supporting bezier tension, monotone and stepped lines.</summary>
    private static string BuildPath(List<(double x, double y)> p, double tension, BitChartSteppedLine stepped,
        BitChartCubicInterpolationMode mode = BitChartCubicInterpolationMode.Default)
    {
        if (p.Count == 0) return "";
        var sb = new StringBuilder();
        sb.Append($"M {BitChartSvg.N(p[0].x)} {BitChartSvg.N(p[0].y)}");
        if (p.Count == 1) return sb.ToString();

        if (stepped != BitChartSteppedLine.False)
        {
            for (int i = 1; i < p.Count; i++)
            {
                var a = p[i - 1]; var b = p[i];
                switch (stepped)
                {
                    case BitChartSteppedLine.Before:
                        sb.Append($" L {BitChartSvg.N(a.x)} {BitChartSvg.N(b.y)} L {BitChartSvg.N(b.x)} {BitChartSvg.N(b.y)}");
                        break;
                    case BitChartSteppedLine.After:
                        sb.Append($" L {BitChartSvg.N(b.x)} {BitChartSvg.N(a.y)} L {BitChartSvg.N(b.x)} {BitChartSvg.N(b.y)}");
                        break;
                    default: // middle
                        double mx = (a.x + b.x) / 2;
                        sb.Append($" L {BitChartSvg.N(mx)} {BitChartSvg.N(a.y)} L {BitChartSvg.N(mx)} {BitChartSvg.N(b.y)} L {BitChartSvg.N(b.x)} {BitChartSvg.N(b.y)}");
                        break;
                }
            }
            return sb.ToString();
        }

        if (mode == BitChartCubicInterpolationMode.Monotone && p.Count > 2)
            return MonotonePath(p);

        if (tension <= 0)
        {
            for (int i = 1; i < p.Count; i++)
                sb.Append($" L {BitChartSvg.N(p[i].x)} {BitChartSvg.N(p[i].y)}");
            return sb.ToString();
        }

        // Cardinal spline -> cubic beziers.
        for (int i = 0; i < p.Count - 1; i++)
        {
            var p0 = p[Math.Max(0, i - 1)];
            var p1 = p[i];
            var p2 = p[i + 1];
            var p3 = p[Math.Min(p.Count - 1, i + 2)];
            double c1x = p1.x + (p2.x - p0.x) / 6 * tension;
            double c1y = p1.y + (p2.y - p0.y) / 6 * tension;
            double c2x = p2.x - (p3.x - p1.x) / 6 * tension;
            double c2y = p2.y - (p3.y - p1.y) / 6 * tension;
            sb.Append($" C {BitChartSvg.N(c1x)} {BitChartSvg.N(c1y)}, {BitChartSvg.N(c2x)} {BitChartSvg.N(c2y)}, {BitChartSvg.N(p2.x)} {BitChartSvg.N(p2.y)}");
        }
        return sb.ToString();
    }

    /// <summary>Monotone cubic interpolation (Fritsch–Carlson) emitted as cubic beziers - never overshoots.</summary>
    private static string MonotonePath(List<(double x, double y)> p)
    {
        int n = p.Count;
        var dx = new double[n - 1];
        var dy = new double[n - 1];
        var d = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            dx[i] = p[i + 1].x - p[i].x;
            dy[i] = p[i + 1].y - p[i].y;
            d[i] = dx[i] != 0 ? dy[i] / dx[i] : 0;
        }

        var m = new double[n];
        m[0] = d[0];
        m[n - 1] = d[n - 2];
        for (int i = 1; i < n - 1; i++)
            m[i] = d[i - 1] * d[i] <= 0 ? 0 : (d[i - 1] + d[i]) / 2;

        for (int i = 0; i < n - 1; i++)
        {
            if (d[i] == 0) { m[i] = 0; m[i + 1] = 0; continue; }
            double a = m[i] / d[i];
            double b = m[i + 1] / d[i];
            double s = a * a + b * b;
            if (s > 9)
            {
                double t = 3 / Math.Sqrt(s);
                m[i] = t * a * d[i];
                m[i + 1] = t * b * d[i];
            }
        }

        var sb = new StringBuilder();
        sb.Append($"M {BitChartSvg.N(p[0].x)} {BitChartSvg.N(p[0].y)}");
        for (int i = 0; i < n - 1; i++)
        {
            double c1x = p[i].x + dx[i] / 3;
            double c1y = p[i].y + m[i] * dx[i] / 3;
            double c2x = p[i + 1].x - dx[i] / 3;
            double c2y = p[i + 1].y - m[i + 1] * dx[i] / 3;
            sb.Append($" C {BitChartSvg.N(c1x)} {BitChartSvg.N(c1y)}, {BitChartSvg.N(c2x)} {BitChartSvg.N(c2y)}, {BitChartSvg.N(p[i + 1].x)} {BitChartSvg.N(p[i + 1].y)}");
        }
        return sb.ToString();
    }

    /// <summary>Builds a rounded-rectangle path with per-corner radii.</summary>
    private static string RoundedRectPath(double x, double y, double w, double h, BitChartBorderRadiusCorners c)
    {
        double max = Math.Min(w, h) / 2;
        double tl = Math.Clamp(c.TopLeft, 0, max);
        double tr = Math.Clamp(c.TopRight, 0, max);
        double br = Math.Clamp(c.BottomRight, 0, max);
        double bl = Math.Clamp(c.BottomLeft, 0, max);
        return $"M {BitChartSvg.N(x + tl)} {BitChartSvg.N(y)} " +
               $"L {BitChartSvg.N(x + w - tr)} {BitChartSvg.N(y)} A {BitChartSvg.N(tr)} {BitChartSvg.N(tr)} 0 0 1 {BitChartSvg.N(x + w)} {BitChartSvg.N(y + tr)} " +
               $"L {BitChartSvg.N(x + w)} {BitChartSvg.N(y + h - br)} A {BitChartSvg.N(br)} {BitChartSvg.N(br)} 0 0 1 {BitChartSvg.N(x + w - br)} {BitChartSvg.N(y + h)} " +
               $"L {BitChartSvg.N(x + bl)} {BitChartSvg.N(y + h)} A {BitChartSvg.N(bl)} {BitChartSvg.N(bl)} 0 0 1 {BitChartSvg.N(x)} {BitChartSvg.N(y + h - bl)} " +
               $"L {BitChartSvg.N(x)} {BitChartSvg.N(y + tl)} A {BitChartSvg.N(tl)} {BitChartSvg.N(tl)} 0 0 1 {BitChartSvg.N(x + tl)} {BitChartSvg.N(y)} Z";
    }
}
