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
        double ringThickness = (ringOuter - ringInner) / datasets.Count;

        var ctx = new BitChartPluginContext
        {
            Scene = scene, Config = _config, IsCartesian = false,
            CenterX = cx, CenterY = cy, InnerRadius = ringInner, OuterRadius = ringOuter
        };
        foreach (var plugin in _options.Plugins.Custom) plugin.BeforeDatasetsDraw(ctx);

        for (int ri = 0; ri < datasets.Count; ri++)
        {
            var (ds, dsIndex) = datasets[ri];
            double outer = ringOuter - ringThickness * ri;
            double inner = outer - ringThickness;

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
                    D = ArcPath(cx + ox, cy + oy, inner, outer, d0, d1),
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
                    D = ArcPath(cx + Math.Cos(mid) * hoverOffset, cy + Math.Sin(mid) * hoverOffset, inner, outer, d0, d1),
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
        var ds = _data.Datasets.FirstOrDefault();
        if (ds is null) return;
        int dsIndex = 0;
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
        if (rOpts.PointLabels.Display && _data.Labels.Count > 0)
            maxR -= PointLabelReserve(rOpts.PointLabels, _data.Labels, n, rotation + sliceAngle / 2, sliceAngle);
        if (maxR <= 0) return;

        var rScale = new BitChartAxisScale(rOpts, horizontal: false) { Culture = Culture };
        rScale.SetDataRange(0, maxVal);
        rScale.SetPixelRange(0, maxR);

        // Radial grid circles.
        if (rOpts.Display && rOpts.Grid.Display)
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
            double r = rScale.PixelFor(v);
            string bg = ResolveBackground(ds, dsIndex, i, true);
            var path = new BitChartSvgPath
            {
                D = ArcPath(cx, cy, 0, r, a0 + halfGap, a1 - halfGap),
                Fill = BitChartColorUtil.WithAlpha(bg, 0.7),
                Stroke = bg, StrokeWidth = arcBorderWidth
            };
            var hoverPath = new BitChartSvgPath
            {
                D = ArcPath(cx, cy, 0, r, a0 + halfGap, a1 - halfGap),
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
                AddDataLabel(scene, v, cx + Math.Cos(mid) * r * 0.6, cy + Math.Sin(mid) * r * 0.6, dsIndex, i);
        }

        // Perimeter category (point) labels.
        if (rOpts.PointLabels.Display && _data.Labels.Count > 0)
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
