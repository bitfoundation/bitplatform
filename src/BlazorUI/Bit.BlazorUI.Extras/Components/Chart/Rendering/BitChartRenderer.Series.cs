using System.Text;

namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private void DrawBars(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale,
        Dictionary<string, BitChartAxisScale> valueScales, List<(BitChartDataset d, int i)> barItems, bool indexIsCategory)
    {
        if (barItems.Count == 0) return;

        // Build slots: stacked datasets share a slot, others get their own.
        var slotKeys = new List<string>();
        var dsSlot = new Dictionary<int, int>();
        foreach (var (ds, i) in barItems)
        {
            bool stacked = valueScales[ds.YAxisID].Options.Stacked;
            string key = stacked ? $"stack:{ds.Stack ?? "default"}:{ds.YAxisID}" : $"ds:{i}";
            if (!slotKeys.Contains(key)) slotKeys.Add(key);
            dsSlot[i] = slotKeys.IndexOf(key);
        }
        int slotCount = Math.Max(1, slotKeys.Count);

        double band = indexScale.BandWidth();
        var first = barItems[0].d;
        double categorySize = band * first.CategoryPercentage;
        double slotSize = categorySize / slotCount;

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

        foreach (var (ds, i) in barItems)
        {
            var vScale = valueScales[ds.YAxisID];
            bool stacked = vScale.Options.Stacked;
            bool stacked100 = stacked && vScale.Options.Stacked100;
            int slot = dsSlot[i];
            double barSize = slotSize * ds.BarPercentage;
            if (ds.BarThickness is { } bt) barSize = bt;
            if (ds.MaxBarThickness is { } mbt) barSize = Math.Min(barSize, mbt);

            int count = ds.Count;
            string? patternFill = ds.BackgroundPattern is { } pat ? RegisterPattern(scene, pat) : null;
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
                        baseVal = Math.Clamp(0, Math.Min(vScale.Min, vScale.Max), Math.Max(vScale.Min, vScale.Max));
                        topVal = value;
                    }
                }

                double centerAlong = indexIsCategory ? indexScale.PixelForIndex(di, true) : indexScale.PixelFor(di);
                double slotCenter = centerAlong - categorySize / 2 + slot * slotSize + slotSize / 2;

                string bg = ResolveBackground(ds, i, di, false, tooltipVal);
                string border = ResolveBorder(ds, i, di, false, tooltipVal);
                if (patternFill is not null) bg = patternFill;
                int signFinal = topVal >= baseVal ? 1 : -1;
                double inflate = ds.InflateAmount ?? 0;
                BitChartSvgRect rect;
                double cx, cy, originX, originY;

                if (IsVertical)
                {
                    double yBase = vScale.PixelFor(baseVal);
                    double yTop = vScale.PixelFor(topVal);
                    rect = new BitChartSvgRect
                    {
                        X = slotCenter - barSize / 2 - inflate,
                        Y = Math.Min(yBase, yTop) - inflate,
                        Width = barSize + inflate * 2,
                        Height = Math.Max(1, Math.Abs(yBase - yTop)) + inflate * 2,
                        Fill = bg,
                        Rx = ds.BorderRadius
                    };
                    cx = slotCenter; cy = yTop;
                    originX = slotCenter; originY = yBase;   // grow from the baseline
                    scene.BarBaseline = yBase;
                }
                else
                {
                    double xBase = vScale.PixelFor(baseVal);
                    double xTop = vScale.PixelFor(topVal);
                    rect = new BitChartSvgRect
                    {
                        X = Math.Min(xBase, xTop) - inflate,
                        Y = slotCenter - barSize / 2 - inflate,
                        Width = Math.Max(1, Math.Abs(xBase - xTop)) + inflate * 2,
                        Height = barSize + inflate * 2,
                        Fill = bg,
                        Rx = ds.BorderRadius
                    };
                    cx = xTop; cy = slotCenter;
                    originX = xBase; originY = slotCenter;   // grow from the baseline
                    scene.BarBaseline = xBase;
                }

                // Per-corner radius emits a rounded path instead of a plain rect.
                BitChartSvgNode shapeNode = rect;
                bool perCorner = ds.BorderRadiusCorners is { } c0 &&
                    (c0.TopLeft > 0 || c0.TopRight > 0 || c0.BottomRight > 0 || c0.BottomLeft > 0);

                // Effective corner radii (explicit per-corner, else uniform BorderRadius) used so the
                // border follows the same rounded outline as the fill.
                BitChartBorderRadiusCorners? roundedCorners = null;
                if (perCorner) roundedCorners = ds.BorderRadiusCorners!.Value;
                else if (ds.BorderRadius > 0) roundedCorners = ds.BorderRadius; // implicit double -> all corners

                var skip = ResolveSkip(isRange ? BitChartBorderSkipped.None : ds.BorderSkipped, IsVertical, signFinal);

                if (perCorner)
                    shapeNode = new BitChartSvgPath
                    {
                        D = RoundedRectPath(rect.X, rect.Y, rect.Width, rect.Height, ds.BorderRadiusCorners!.Value),
                        Fill = bg
                    };

                // Border honoring borderSkipped. Rounded bars get a matching rounded border path so the
                // stroke follows the corner radius instead of cutting square corners.
                BitChartSvgNode? borderNode = null;
                if (ds.BorderWidth > 0)
                {
                    if (roundedCorners is { } rc)
                    {
                        borderNode = new BitChartSvgPath
                        {
                            D = RoundedBarBorderPath(rect.X, rect.Y, rect.Width, rect.Height, rc, skip),
                            Fill = "none", Stroke = border, StrokeWidth = ds.BorderWidth
                        };
                    }
                    else if (skip == BitChartBorderSkipped.None)
                    {
                        rect.Stroke = border;
                        rect.StrokeWidth = ds.BorderWidth;
                    }
                    else
                    {
                        // Drawn as part of the element so it animates together with the fill.
                        borderNode = new BitChartSvgPath
                        {
                            D = BarBorderPath(rect, skip), Fill = "none", Stroke = border, StrokeWidth = ds.BorderWidth
                        };
                    }
                }

                string text = isRange
                    ? $"{(ds.Label is null ? "" : ds.Label + ": ")}[{baseVal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}, {topVal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}]"
                    : BuildItemText(ds, i, di, tooltipVal, bg);

                // Each bar grows from its own baseline in the correct direction (size change, not a slide).
                string enterAnim = IsVertical ? "bc-scale-y" : "bc-scale-x";

                scene.Elements.Add(new BitChartDataElement
                {
                    Shape = shapeNode,
                    BorderShape = borderNode,
                    EnterAnim = enterAnim,
                    AnimOriginX = originX,
                    AnimOriginY = originY,
                    DatasetIndex = i,
                    DataIndex = di,
                    CenterX = cx,
                    CenterY = cy,
                    Value = tooltipVal,
                    SeriesLabel = ds.Label,
                    Tooltip = new BitChartTooltipInfo
                    {
                        Title = di < _data.Labels.Count ? _data.Labels[di] : null,
                        AnchorX = cx,
                        AnchorY = IsVertical ? Math.Min(rect.Y, cy) : cy,
                        Items = { new BitChartTooltipItem { Color = bg, Text = text } }
                    }
                });

                if (!isRange) AddDataLabel(scene, tooltipVal, cx, IsVertical ? rect.Y - 4 : cx, i, di);
            }
        }
    }

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

        for (int di = 0; di < ds.Data.Count; di++)
        {
            if (ds.Data[di] is not { } v)
            {
                if (!ds.SpanGaps) { FlushLine(scene, plot, vScale, ds, dsIndex, pts, indexScale, indexIsCategory, centered); pts.Clear(); }
                continue;
            }
            double x = indexIsCategory ? indexScale.PixelForIndex(di, centered) : indexScale.PixelFor(di);
            double y = vScale.PixelFor(v);
            pts.Add((x, y, di, v));
        }
        FlushLine(scene, plot, vScale, ds, dsIndex, pts, indexScale, indexIsCategory, centered);
    }

    private void FlushLine(BitChartScene scene, BitChartArea plot, BitChartAxisScale vScale, BitChartDataset ds, int dsIndex,
        List<(double x, double y, int di, double v)> pts,
        BitChartAxisScale? indexScale = null, bool indexIsCategory = false, bool centered = false)
    {
        if (pts.Count == 0) return;

        var dec = _options.Plugins.Decimation;
        if (dec.Enabled && pts.Count > dec.Threshold && dec.Samples >= 2 && dec.Samples < pts.Count)
            pts = BitChartDecimation.Lttb(pts, dec.Samples);

        string border = ResolveBorder(ds, dsIndex, 0, false);
        var xy = pts.Select(p => (p.x, p.y)).ToList();
        string d = BuildPath(xy, ds.Tension, ds.Stepped, ds.CubicInterpolationMode);

        bool progressive = _options.Animation.Animate && _options.Animation.Progressive;
        if (progressive) scene.ProgressiveDraw = true;

        if (ds.Fill != BitChartFillMode.None && ds.ShowLine)
        {
            string? fillD = null;

            // Fill to another dataset's line (range area).
            if (ds.Fill == BitChartFillMode.Dataset && ds.FillTargetIndex is { } ti
                && ti >= 0 && ti < _data.Datasets.Count && indexScale is not null)
            {
                var target = ComputeLinePoints(_data.Datasets[ti], indexScale, vScale, indexIsCategory, centered);
                if (target.Count > 0)
                    fillD = AreaBetween(xy, ds.Tension, ds.Stepped, target.Select(p => (p.x, p.y)).ToList());
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

            scene.Series.Add(new BitChartSvgPath { D = fillD, Fill = ResolveFill(scene, ds, border, plot), Stroke = null, AnimateFade = progressive });
        }

        if (ds.ShowLine)
        {
            if (ds.Segment is { } seg)
            {
                // Draw each consecutive segment with its own resolved style.
                double defWidth = ds.BorderWidth <= 1 ? _options.Elements.LineBorderWidth : ds.BorderWidth;
                for (int k = 0; k < pts.Count - 1; k++)
                {
                    var a = pts[k];
                    var b = pts[k + 1];
                    var sctx = new BitChartSegmentContext(a.di, b.di, a.v, b.v);
                    string color = seg.BorderColor?.Invoke(sctx) ?? border;
                    double width = seg.BorderWidth?.Invoke(sctx) ?? defWidth;
                    var dash = seg.BorderDash?.Invoke(sctx);
                    scene.Series.Add(new BitChartSvgPath
                    {
                        D = $"M {BitChartSvg.N(a.x)} {BitChartSvg.N(a.y)} L {BitChartSvg.N(b.x)} {BitChartSvg.N(b.y)}",
                        Fill = "none", Stroke = color, StrokeWidth = width,
                        Dash = dash is null ? "" : BitChartSvg.Dash(dash),
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
                    D = d, Fill = "none", Stroke = border,
                    StrokeWidth = ds.BorderWidth <= 1 ? _options.Elements.LineBorderWidth : ds.BorderWidth,
                    Dash = BitChartSvg.Dash(ds.BorderDash), LineCap = ds.BorderCapStyle, LineJoin = ds.BorderJoinStyle,
                    // Draw-on reveals the stroke left to right; dashed strokes can't (dasharray is in use), so they fade.
                    AnimateDraw = progressive && !dashed,
                    AnimateFade = progressive && dashed
                });
            }
        }

        if (ds.PointRadius > 0 || ds.PointStyle != BitChartPointStyle.None)
            foreach (var p in pts)
                AddPoint(scene, ds, dsIndex, p.di, p.x, p.y, p.v, ds.PointRadius, border);
    }

    /// <summary>Resolves an area fill paint (pattern, gradient, explicit color, or translucent border).</summary>
    private string ResolveFill(BitChartScene scene, BitChartDataset ds, string border, BitChartArea plot)
    {
        if (ds.BackgroundPattern is { } pat) return RegisterPattern(scene, pat);
        if (ds.FillGradient is { Stops.Count: > 0 } g) return RegisterGradient(scene, g);
        return ds.FillColor ?? BitChartColorUtil.WithAlpha(border, 0.2);
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
            var cumulative = new Dictionary<int, double>();

            foreach (var (ds, i) in group)
            {
                var topPts = new List<(double x, double y, int di, double v)>();
                var basePts = new List<(double x, double y)>();
                for (int di = 0; di < ds.Data.Count; di++)
                {
                    if (ds.Data[di] is not { } v) continue;
                    double baseVal = cumulative.GetValueOrDefault(di, 0);
                    double topVal = baseVal + v;
                    cumulative[di] = topVal;
                    double x = indexIsCategory ? indexScale.PixelForIndex(di, centered) : indexScale.PixelFor(di);
                    topPts.Add((x, vScale.PixelFor(topVal), di, topVal));
                    basePts.Add((x, vScale.PixelFor(baseVal)));
                }
                if (topPts.Count == 0) continue;

                string border = ResolveBorder(ds, i, 0, false);
                var topXy = topPts.Select(p => (p.x, p.y)).ToList();

                bool progressive = _options.Animation.Animate && _options.Animation.Progressive;
                if (progressive) scene.ProgressiveDraw = true;
                bool dashed = ds.BorderDash is { Count: > 0 };

                if (ds.Fill != BitChartFillMode.None)
                {
                    string fillD = AreaBetween(topXy, ds.Tension, ds.Stepped, basePts);
                    scene.Series.Add(new BitChartSvgPath { D = fillD, Fill = ResolveFill(scene, ds, border, plot), Stroke = null, AnimateFade = progressive });
                }

                scene.Series.Add(new BitChartSvgPath
                {
                    D = BuildPath(topXy, ds.Tension, ds.Stepped), Fill = "none", Stroke = border,
                    StrokeWidth = ds.BorderWidth <= 1 ? _options.Elements.LineBorderWidth : ds.BorderWidth,
                    Dash = BitChartSvg.Dash(ds.BorderDash), LineCap = ds.BorderCapStyle, LineJoin = ds.BorderJoinStyle,
                    AnimateDraw = progressive && !dashed,
                    AnimateFade = progressive && dashed
                });

                if (ds.PointRadius > 0)
                    foreach (var p in topPts)
                        AddPoint(scene, ds, i, p.di, p.x, p.y, ds.Data[p.di] ?? 0, ds.PointRadius, border);
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
            double r = bubble ? (p.R ?? 5) : ds.PointRadius <= 3 ? 4 : ds.PointRadius;
            AddPoint(scene, ds, dsIndex, di, x, y, p.Y, r, border, p.X);
        }
    }

    private void AddPoint(BitChartScene scene, BitChartDataset ds, int dsIndex, int di,
        double x, double y, double value, double radius, string border, double? xValue = null)
    {
        var ctx = Ctx(ds, dsIndex, di, value);
        bool active = _state.Active == (dsIndex, di);

        double r = ds.PointRadiusFn?.Invoke(ctx) ?? radius;
        var style = ds.PointStyleFn?.Invoke(ctx) ?? ds.PointStyle;
        string fill = ds.PointBackgroundColorFn?.Invoke(ctx) ?? ds.PointBackgroundColor ?? ResolveBackground(ds, dsIndex, di, false, value);
        string stroke = ds.PointBorderColorFn?.Invoke(ctx) ?? ds.PointBorderColor ?? border;
        double bw = ds.PointBorderWidth;

        if (active)
        {
            r = Math.Max(r, ds.PointHoverRadius);
            if (ds.PointHoverBackgroundColor is { } hb) fill = hb;
            if (ds.PointHoverBorderColor is { } hbc) stroke = hbc;
            if (ds.PointHoverBorderWidth is { } hbw) bw = hbw;
        }

        var shape = BitChartPointShapes.Build(style, x, y, r, fill, stroke, bw);
        if (shape is null) return;

        bool cartesian = _config.Type is not (BitChartType.Pie or BitChartType.Doughnut or BitChartType.PolarArea or BitChartType.Radar);

        string text = xValue is { } xv
            ? $"({xv.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}, {value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)})"
            : BuildItemText(ds, dsIndex, di, value, fill);

        scene.Elements.Add(new BitChartDataElement
        {
            Shape = shape,
            EnterAnim = cartesian ? "bc-pop" : null,
            AnimOriginX = x,
            AnimOriginY = y,
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
    }

    private void AddDataLabel(BitChartScene scene, double value, double x, double y, int dsIndex = 0, int dataIndex = 0)
    {
        var dl = _options.Plugins.DataLabels;
        if (!dl.Display) return;
        if (dl.DisplayFn is { } show && !show(value, dsIndex, dataIndex)) return;

        string text = dl.FormatterCtx?.Invoke(value, dsIndex, dataIndex)
            ?? dl.Formatter?.Invoke(value)
            ?? value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        if (dl.BackgroundColor is { } bgc)
        {
            double w = BitChartTextMeasure.Width(text, dl.Font.Size, dl.Font.Weight) + dl.Padding * 2;
            double h = dl.Font.Size + dl.Padding * 2;
            scene.Foreground.Add(new BitChartSvgRect
            {
                X = x - w / 2, Y = y - h / 2, Width = w, Height = h, Rx = dl.BorderRadius, Fill = bgc
            });
        }

        scene.Foreground.Add(new BitChartSvgText
        {
            X = x, Y = y,
            Text = text,
            Fill = dl.Color, FontFamily = dl.Font.Family, FontSize = dl.Font.Size, FontWeight = dl.Font.Weight,
            Anchor = "middle", Baseline = dl.BackgroundColor is null ? "auto" : "central", Rotation = dl.Rotation
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
