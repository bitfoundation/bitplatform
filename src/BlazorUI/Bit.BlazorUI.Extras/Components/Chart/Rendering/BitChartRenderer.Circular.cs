namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private void RenderCircular(BitChartScene scene)
    {
        var area = ContentArea();
        double cx = area.CenterX;
        double cy = area.CenterY;
        double maxR = Math.Min(area.Width, area.Height) / 2 - 4;
        if (maxR <= 0) return;

        if (_config.Type == BitChartType.PolarArea)
        {
            RenderPolarArea(scene, cx, cy, maxR);
            return;
        }

        var datasets = _data.Datasets
            .Select((d, i) => (ds: d, index: i))
            .Where(t => !t.ds.Hidden && !_state.IsDatasetHidden(t.index))
            .ToList();
        if (datasets.Count == 0) return;

        double rotation = _options.RotationDegrees * Math.PI / 180;
        double circumference = _options.CircumferenceDegrees * Math.PI / 180;
        // Clamped: a cutout at or past 100% would put the inner radius outside the outer one and
        // invert every arc.
        double cutout = _config.Type == BitChartType.Doughnut
            ? Math.Clamp(_options.CutoutPercentage / 100.0, 0, 0.95)
            : 0;

        double ringOuter = maxR;
        double ringInner = maxR * cutout;

        // Ring thickness is shared out by weight, so one dataset can be given a thicker band than the
        // others. With every weight left at its default of 1 this is an even split, exactly as before.
        double ringSpan = ringOuter - ringInner;
        double totalWeight = datasets.Sum(t => Math.Max(0, t.ds.Weight));
        if (totalWeight <= 0) totalWeight = datasets.Count;

        var ctx = new BitChartPluginContext
        {
            Scene = scene, Config = _config, IsCartesian = false,
            CenterX = cx, CenterY = cy, InnerRadius = ringInner, OuterRadius = ringOuter
        };
        foreach (var plugin in _options.Plugins.Custom) plugin.BeforeDatasetsDraw(ctx);

        double ringCursor = ringOuter;
        for (int ri = 0; ri < datasets.Count; ri++)
        {
            var (ds, dsIndex) = datasets[ri];
            double outer = ringCursor;
            double inner = outer - ringSpan * (Math.Max(0, ds.Weight) / totalWeight);
            ringCursor = inner;

            double total = 0;
            for (int i = 0; i < ds.Data.Count; i++)
                if (!_state.IsIndexHidden(i) && ds.Data[i] is { } v) total += Math.Abs(v);
            if (total <= 0) continue;

            double arcBorderWidth = ResolveBorderWidth(ds, _config.Type, dsIndex);
            // The angular half-gap that realizes the requested pixel spacing at the outer edge.
            double halfGap = ds.SpacingArc > 0 && outer > 0 ? ds.SpacingArc / 2 / outer : 0;

            double angle = rotation;
            for (int i = 0; i < ds.Data.Count; i++)
            {
                if (_state.IsIndexHidden(i) || ds.Data[i] is not { } v) continue;
                double slice = Math.Abs(v) / total * circumference;
                double a0 = angle;
                double a1 = angle + slice;
                angle = a1;

                // Apply the inter-arc gap without letting it swallow a very thin slice.
                double gap = Math.Min(halfGap, Math.Max(0, (a1 - a0) / 4));
                double d0 = a0 + gap, d1 = a1 - gap;

                string bg = ResolveBackground(ds, dsIndex, i, true);
                string arcBorder = ResolveBorder(ds, dsIndex, i, true, v, fallbackToBackground: false);
                string borderColor = HasExplicitBorder(ds) ? arcBorder : _options.Elements.ArcBorderColor;
                double mid = (a0 + a1) / 2;
                double ox = ds.Offset > 0 ? Math.Cos(mid) * ds.Offset : 0;
                double oy = ds.Offset > 0 ? Math.Sin(mid) * ds.Offset : 0;

                var path = new BitChartSvgPath
                {
                    D = ArcPath(cx + ox, cy + oy, inner, outer, d0, d1, ds.BorderRadius),
                    Fill = bg,
                    Stroke = borderColor,
                    StrokeWidth = arcBorderWidth
                };

                // Hover: the arc is pushed further out and repainted with the hover colors. Precomputed
                // so hovering an arc never costs a full re-layout.
                double hoverOffset = ds.Offset + ds.HoverOffset;
                string hoverBg = ds.HoverBackgroundColor
                    ?? (ds.BackgroundColorFn is not null ? ResolveBackground(ds, dsIndex, i, true, v, active: true) : BitChartColorUtil.Adjust(bg, 0.08));
                var hoverPath = new BitChartSvgPath
                {
                    D = ArcPath(cx + Math.Cos(mid) * hoverOffset, cy + Math.Sin(mid) * hoverOffset, inner, outer, d0, d1, ds.BorderRadius),
                    Fill = hoverBg,
                    Stroke = ds.HoverBorderColor ?? borderColor,
                    StrokeWidth = ds.HoverBorderWidth ?? arcBorderWidth
                };

                double pct = Math.Abs(v) / total * 100;
                string label = i < _data.Labels.Count ? _data.Labels[i] : ds.Label ?? "";
                string text = _options.Plugins.Tooltip.Callbacks.Label is not null || _options.Plugins.Tooltip.LabelFormatter is not null
                    ? BuildItemText(ds, dsIndex, i, v, bg)
                    : $"{FormatNumber(v, "0.##")} ({FormatNumber(pct, "0.#")}%)";

                scene.Elements.Add(new BitChartDataElement
                {
                    Shape = path,
                    HoverShape = hoverPath,
                    DatasetIndex = dsIndex,
                    DataIndex = i,
                    CenterX = cx + Math.Cos(mid) * (inner + outer) / 2,
                    CenterY = cy + Math.Sin(mid) * (inner + outer) / 2,
                    Value = v,
                    SeriesLabel = label,
                    Tooltip = new BitChartTooltipInfo
                    {
                        Title = i < _data.Labels.Count ? _data.Labels[i] : null,
                        AnchorX = cx + Math.Cos(mid) * outer,
                        AnchorY = cy + Math.Sin(mid) * outer,
                        Items = { new BitChartTooltipItem { Color = bg, Text = text } }
                    }
                });

                if (_options.Plugins.DataLabels.Display && slice > 0.15)
                {
                    var dl = _options.Plugins.DataLabels;
                    double lr = dl.Anchor switch
                    {
                        BitChartAlign.Start => inner + (inner > 0 ? 0 : 0.35 * outer),
                        BitChartAlign.End => outer,
                        _ => (inner + outer) / 2
                    };
                    lr += AlignShift(dl, 1);
                    AddDataLabel(scene, v, cx + ox + Math.Cos(mid) * lr, cy + oy + Math.Sin(mid) * lr, dsIndex, i);
                }
            }
        }

        foreach (var plugin in _options.Plugins.Custom) plugin.AfterDatasetsDraw(ctx);
    }

    private void RenderPolarArea(BitChartScene scene, double cx, double cy, double maxR)
    {
        // The polar scale is shared by the whole chart, so it draws the first *visible* dataset -
        // hiding it from the legend has to empty the chart rather than leave it drawn.
        int dsIndex = -1;
        for (int k = 0; k < _data.Datasets.Count; k++)
            if (!IsHidden(k, _data.Datasets[k])) { dsIndex = k; break; }
        if (dsIndex < 0) return;
        var ds = _data.Datasets[dsIndex];
        int n = ds.Data.Count;
        if (n == 0) return;

        double maxVal = 0;
        for (int i = 0; i < n; i++)
            if (!_state.IsIndexHidden(i) && ds.Data[i] is { } v) maxVal = Math.Max(maxVal, v);
        if (maxVal <= 0) maxVal = 1;

        var rOpts = Scale(RadialScaleId);
        double sliceAngle = 2 * Math.PI / n;
        double rotation = (_options.RotationDegrees + rOpts.StartAngle) * Math.PI / 180;

        // Reserve room for perimeter point labels, measured the same way the radar chart does.
        if (PointLabelsVisible(rOpts) && _data.Labels.Count > 0)
            maxR -= PointLabelReserve(rOpts.PointLabels, _data.Labels, n, rotation + sliceAngle / 2, sliceAngle);
        if (maxR <= 0) return;

        var rScale = new BitChartAxisScale(rOpts, horizontal: false) { Culture = Culture };
        rScale.SetDataRange(0, maxVal);
        rScale.SetPixelRange(0, maxR);

        var pctx = new BitChartPluginContext
        {
            Scene = scene, Config = _config, IsCartesian = false,
            CenterX = cx, CenterY = cy, InnerRadius = 0, OuterRadius = maxR
        };
        foreach (var plugin in _options.Plugins.Custom) plugin.BeforeDatasetsDraw(pctx);

        // Radial grid circles.
        if (ScaleVisible(rOpts) && rOpts.Grid.Display)
        {
            foreach (var t in rScale.Ticks)
            {
                double rr = t.Pixel;
                if (rr <= 0) continue;
                scene.Background.Add(new BitChartSvgCircle { Cx = cx, Cy = cy, R = rr, Fill = "none", Stroke = rOpts.Grid.Color, StrokeWidth = rOpts.Grid.LineWidth });
                if (rOpts.Ticks.Display) AddRadialTickLabel(scene, rOpts, cx, cy, rr, t.Label);
            }
        }

        double arcBorderWidth = ResolveBorderWidth(ds, BitChartType.PolarArea, dsIndex);
        double angle = rotation;
        for (int i = 0; i < n; i++)
        {
            if (_state.IsIndexHidden(i) || ds.Data[i] is not { } v) { angle += sliceAngle; continue; }
            double a0 = angle, a1 = angle + sliceAngle;
            angle = a1;
            double halfGap = ds.SpacingArc > 0 && maxR > 0 ? Math.Min(ds.SpacingArc / 2 / maxR, sliceAngle / 4) : 0;
            // A value below the scale minimum maps to a negative radius, which would draw the wedge
            // inside out through the center; a polar wedge simply has no length there.
            double r = Math.Clamp(rScale.PixelFor(v), 0, maxR);
            string bg = ResolveBackground(ds, dsIndex, i, true);
            var path = new BitChartSvgPath
            {
                D = ArcPath(cx, cy, 0, r, a0 + halfGap, a1 - halfGap, ds.BorderRadius),
                Fill = BitChartColorUtil.WithAlpha(bg, 0.7),
                Stroke = bg, StrokeWidth = arcBorderWidth
            };
            var hoverPath = new BitChartSvgPath
            {
                D = ArcPath(cx, cy, 0, r, a0 + halfGap, a1 - halfGap, ds.BorderRadius),
                Fill = BitChartColorUtil.WithAlpha(ds.HoverBackgroundColor ?? BitChartColorUtil.Adjust(bg, -0.1), 0.85),
                Stroke = ds.HoverBorderColor ?? bg, StrokeWidth = ds.HoverBorderWidth ?? arcBorderWidth
            };
            double mid = (a0 + a1) / 2;
            scene.Elements.Add(new BitChartDataElement
            {
                Shape = path,
                HoverShape = hoverPath,
                DatasetIndex = dsIndex,
                DataIndex = i,
                CenterX = cx + Math.Cos(mid) * r / 2,
                CenterY = cy + Math.Sin(mid) * r / 2,
                Value = v,
                SeriesLabel = i < _data.Labels.Count ? _data.Labels[i] : ds.Label,
                Tooltip = new BitChartTooltipInfo
                {
                    Title = i < _data.Labels.Count ? _data.Labels[i] : null,
                    AnchorX = cx + Math.Cos(mid) * r,
                    AnchorY = cy + Math.Sin(mid) * r,
                    Items = { new BitChartTooltipItem { Color = bg, Text = BuildItemText(ds, dsIndex, i, v, bg) } }
                }
            });

            if (_options.Plugins.DataLabels.Display)
            {
                // Placed the way a doughnut places its labels, with the wedge running from the center
                // out to its own radius.
                var dl = _options.Plugins.DataLabels;
                double lr = dl.Anchor switch
                {
                    BitChartAlign.Start => 0.35 * r,
                    BitChartAlign.End => r,
                    _ => r / 2
                };
                lr += AlignShift(dl, 1);
                AddDataLabel(scene, v, cx + Math.Cos(mid) * lr, cy + Math.Sin(mid) * lr, dsIndex, i);
            }
        }

        // Perimeter category (point) labels.
        if (PointLabelsVisible(rOpts) && _data.Labels.Count > 0)
        {
            var pl = rOpts.PointLabels;
            double a = rotation;
            for (int i = 0; i < n; i++)
            {
                double mid = a + sliceAngle / 2;
                a += sliceAngle;
                if (i >= _data.Labels.Count) continue;
                double lr = maxR + pl.Padding + 6;
                double lx = cx + Math.Cos(mid) * lr;
                double ly = cy + Math.Sin(mid) * lr;
                string anchor = Math.Abs(Math.Cos(mid)) < 0.3 ? "middle" : Math.Cos(mid) > 0 ? "start" : "end";
                string label = pl.Callback?.Invoke(_data.Labels[i], i) ?? _data.Labels[i];
                scene.Background.Add(new BitChartSvgText
                {
                    X = lx, Y = ly, Text = label, Fill = pl.Color,
                    FontSize = pl.Font.Size, FontFamily = pl.Font.Family, FontWeight = pl.Font.Weight,
                    Anchor = anchor, Baseline = "central"
                });
            }
        }

        foreach (var plugin in _options.Plugins.Custom) plugin.AfterDatasetsDraw(pctx);
    }

    /// <summary>
    /// How much room the perimeter labels of a radial chart need. Each label is measured and projected
    /// onto its own angle - a label at the side sticks out by its full width, one at the top or bottom
    /// only by half - so the plot shrinks by what the longest label actually needs and no more.
    /// </summary>
    private static double PointLabelReserve(BitChartPointLabelOptions pl, List<string> labels, int count,
        double startAngle, double angleStep)
    {
        double needed = pl.Font.LineHeightPx;
        for (int i = 0; i < count && i < labels.Count; i++)
        {
            double a = startAngle + angleStep * i;
            double w = BitChartTextMeasure.Width(pl.Callback?.Invoke(labels[i], i) ?? labels[i], pl.Font.Size, pl.Font.Weight);
            double cos = Math.Abs(Math.Cos(a));
            // Near-vertical labels are centered on the spoke, so only half of them sticks out.
            double horizontal = cos * (cos < 0.3 ? w / 2 : w);
            double vertical = Math.Abs(Math.Sin(a)) * pl.Font.LineHeightPx;
            needed = Math.Max(needed, horizontal + vertical);
        }
        return needed + pl.Padding + 6;
    }

    /// <summary>Draws one radial tick label, optionally over a backdrop so it stays readable on the grid.</summary>
    private static void AddRadialTickLabel(BitChartScene scene, BitChartScaleOptions rOpts, double cx, double cy, double rr, string label)
    {
        if (rOpts.ShowLabelBackdrop)
        {
            double w = BitChartTextMeasure.Width(label, rOpts.Ticks.Font.Size) + 4;
            scene.Background.Add(new BitChartSvgRect
            {
                X = cx + 2, Y = cy - rr - rOpts.Ticks.Font.Size * 0.55,
                Width = w, Height = rOpts.Ticks.Font.Size + 2, Fill = rOpts.BackdropColor
            });
        }
        scene.Background.Add(new BitChartSvgText
        {
            X = cx + 4, Y = cy - rr, Text = label, Fill = rOpts.Ticks.Color,
            FontSize = rOpts.Ticks.Font.Size, FontFamily = rOpts.Ticks.Font.Family, Anchor = "start", Baseline = "central"
        });
    }

    /// <summary>
    /// Builds an arc path, rounding its corners when the dataset asks for it. A zero radius - or an arc
    /// that has come round to a full circle, which has no corners to round - falls through to the plain
    /// path, so nothing changes for the charts that never set one.
    /// </summary>
    private static string ArcPath(double cx, double cy, double inner, double outer, double a0, double a1, double cornerRadius)
        => cornerRadius > 0 && a1 - a0 < 2 * Math.PI - 1e-3
            ? RoundedArcPath(cx, cy, inner, outer, a0, a1, cornerRadius)
            : ArcPath(cx, cy, inner, outer, a0, a1);

    /// <summary>
    /// Builds an arc/ring path whose corners are rounded, mirroring Chart.js's arc <c>borderRadius</c>.
    /// Each corner is cut back along both edges that meet there and reconnected with a quadratic curve
    /// through the true corner point, which reads as a fillet at any radius and cannot self-intersect:
    /// the radius is clamped to half the ring's thickness and half its arc length first. A pie arc has
    /// no inner edge, so only the two outer corners are rounded - its point sits at the center.
    /// </summary>
    private static string RoundedArcPath(double cx, double cy, double inner, double outer, double a0, double a1, double radius)
    {
        double span = a1 - a0;
        double thickness = outer - Math.Max(0, inner);
        if (span <= 0 || thickness <= 0) return ArcPath(cx, cy, inner, outer, a0, a1);

        // The outer edge is the longest, so it decides how much of a corner there is room for.
        double r = Math.Min(radius, thickness / 2);
        r = Math.Min(r, span * outer / 2);
        if (r <= 0.01) return ArcPath(cx, cy, inner, outer, a0, a1);

        (double X, double Y) P(double rad, double ang) => (cx + rad * Math.Cos(ang), cy + rad * Math.Sin(ang));
        static string M((double X, double Y) p) => $"M {BitChartSvg.N(p.X)} {BitChartSvg.N(p.Y)} ";
        static string L((double X, double Y) p) => $"L {BitChartSvg.N(p.X)} {BitChartSvg.N(p.Y)} ";
        static string Q((double X, double Y) c, (double X, double Y) p)
            => $"Q {BitChartSvg.N(c.X)} {BitChartSvg.N(c.Y)}, {BitChartSvg.N(p.X)} {BitChartSvg.N(p.Y)} ";
        string A(double rad, (double X, double Y) p, int sweep, double from, double to)
            => $"A {BitChartSvg.N(rad)} {BitChartSvg.N(rad)} 0 {(Math.Abs(to - from) > Math.PI ? 1 : 0)} {sweep} {BitChartSvg.N(p.X)} {BitChartSvg.N(p.Y)} ";

        // Angular size of the corner on each edge: the same arc length r, so a tighter radius covers
        // more angle on the inner edge than on the outer one.
        double outerCut = r / outer;
        double oa0 = a0 + outerCut, oa1 = a1 - outerCut;
        if (oa1 < oa0) { double mid = (a0 + a1) / 2; oa0 = oa1 = mid; }

        var sb = new System.Text.StringBuilder();
        if (inner <= 0.01)
        {
            // Pie wedge: center → rounded outer start → outer arc → rounded outer end → center.
            sb.Append(M((cx, cy)));
            sb.Append(L(P(outer - r, a0)));
            sb.Append(Q(P(outer, a0), P(outer, oa0)));
            sb.Append(A(outer, P(outer, oa1), 1, oa0, oa1));
            sb.Append(Q(P(outer, a1), P(outer - r, a1)));
            sb.Append('Z');
            return sb.ToString();
        }

        double innerCut = Math.Min(r / inner, span / 2);
        double ia0 = a0 + innerCut, ia1 = a1 - innerCut;
        if (ia1 < ia0) { double mid = (a0 + a1) / 2; ia0 = ia1 = mid; }

        sb.Append(M(P(inner + r, a0)));
        sb.Append(L(P(outer - r, a0)));
        sb.Append(Q(P(outer, a0), P(outer, oa0)));
        sb.Append(A(outer, P(outer, oa1), 1, oa0, oa1));
        sb.Append(Q(P(outer, a1), P(outer - r, a1)));
        sb.Append(L(P(inner + r, a1)));
        sb.Append(Q(P(inner, a1), P(inner, ia1)));
        sb.Append(A(inner, P(inner, ia0), 0, ia1, ia0));
        sb.Append(Q(P(inner, a0), P(inner + r, a0)));
        sb.Append('Z');
        return sb.ToString();
    }

    /// <summary>Builds an SVG arc/ring path. Handles full circles.</summary>
    private static string ArcPath(double cx, double cy, double inner, double outer, double a0, double a1)
    {
        double span = a1 - a0;
        bool full = span >= 2 * Math.PI - 1e-3;
        if (full) a1 = a0 + 2 * Math.PI - 1e-3;

        int large = (a1 - a0) > Math.PI ? 1 : 0;
        double x0o = cx + outer * Math.Cos(a0), y0o = cy + outer * Math.Sin(a0);
        double x1o = cx + outer * Math.Cos(a1), y1o = cy + outer * Math.Sin(a1);

        if (inner <= 0.01)
        {
            return $"M {BitChartSvg.N(cx)} {BitChartSvg.N(cy)} L {BitChartSvg.N(x0o)} {BitChartSvg.N(y0o)} " +
                   $"A {BitChartSvg.N(outer)} {BitChartSvg.N(outer)} 0 {large} 1 {BitChartSvg.N(x1o)} {BitChartSvg.N(y1o)} Z";
        }

        double x0i = cx + inner * Math.Cos(a0), y0i = cy + inner * Math.Sin(a0);
        double x1i = cx + inner * Math.Cos(a1), y1i = cy + inner * Math.Sin(a1);
        return $"M {BitChartSvg.N(x0o)} {BitChartSvg.N(y0o)} " +
               $"A {BitChartSvg.N(outer)} {BitChartSvg.N(outer)} 0 {large} 1 {BitChartSvg.N(x1o)} {BitChartSvg.N(y1o)} " +
               $"L {BitChartSvg.N(x1i)} {BitChartSvg.N(y1i)} " +
               $"A {BitChartSvg.N(inner)} {BitChartSvg.N(inner)} 0 {large} 0 {BitChartSvg.N(x0i)} {BitChartSvg.N(y0i)} Z";
    }
}
