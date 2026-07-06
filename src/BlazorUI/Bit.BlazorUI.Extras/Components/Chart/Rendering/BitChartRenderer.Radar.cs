
namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private void RenderRadar(BitChartScene scene)
    {
        var area = ContentArea();
        int n = _data.Labels.Count;
        if (n == 0) return;

        var rOpts = _options.Scales["r"];

        // Reserve room for the point labels around the perimeter.
        double labelPad = rOpts.PointLabels.Display
            ? rOpts.PointLabels.Font.Size + rOpts.PointLabels.Padding + 14
            : 8;
        double cx = area.CenterX;
        double cy = area.CenterY;
        double maxR = Math.Min(area.Width, area.Height) / 2 - labelPad;
        if (maxR <= 0) return;

        double min = 0, max = double.NegativeInfinity;
        for (int d = 0; d < _data.Datasets.Count; d++)
        {
            var ds = _data.Datasets[d];
            if (IsHidden(d, ds)) continue;
            foreach (var v in ds.Data) if (v is { } val) { max = Math.Max(max, val); min = Math.Min(min, val); }
        }
        if (double.IsInfinity(max)) max = 1;

        var rScale = new BitChartAxisScale(rOpts, horizontal: false);
        rScale.SetDataRange(rOpts.BeginAtZero ? 0 : min, max);
        rScale.SetPixelRange(0, maxR);

        double angleStep = 2 * Math.PI / n;
        double start = -Math.PI / 2 + rOpts.StartAngle * Math.PI / 180;

        // Grid rings (polygons by default, circles when grid.circular).
        if (rOpts.Display && rOpts.Grid.Display)
        {
            foreach (var t in rScale.Ticks)
            {
                double rr = t.Pixel;
                if (rr <= 0.01) continue;
                if (rOpts.Grid.Circular)
                {
                    scene.Background.Add(new BitChartSvgCircle { Cx = cx, Cy = cy, R = rr, Fill = "none", Stroke = rOpts.Grid.Color, StrokeWidth = rOpts.Grid.LineWidth });
                }
                else
                {
                    var poly = new BitChartSvgPolygon { Fill = "none", Stroke = rOpts.Grid.Color, StrokeWidth = rOpts.Grid.LineWidth };
                    for (int i = 0; i < n; i++)
                    {
                        double a = start + angleStep * i;
                        poly.Points.Add((cx + Math.Cos(a) * rr, cy + Math.Sin(a) * rr));
                    }
                    scene.Background.Add(poly);
                }
            }
        }

        // Angle lines + point (category) labels.
        for (int i = 0; i < n; i++)
        {
            double a = start + angleStep * i;
            double ex = cx + Math.Cos(a) * maxR;
            double ey = cy + Math.Sin(a) * maxR;
            if (rOpts.Display && rOpts.AngleLines)
                scene.Background.Add(new BitChartSvgLine
                {
                    X1 = cx, Y1 = cy, X2 = ex, Y2 = ey,
                    Stroke = rOpts.AngleLineColor, StrokeWidth = rOpts.AngleLineWidth,
                    Dash = BitChartSvg.Dash(rOpts.AngleLineDash)
                });

            if (rOpts.PointLabels.Display)
            {
                var pl = rOpts.PointLabels;
                double lx = cx + Math.Cos(a) * (maxR + pl.Padding + 4);
                double ly = cy + Math.Sin(a) * (maxR + pl.Padding + 4);
                string anchor = Math.Abs(Math.Cos(a)) < 0.3 ? "middle" : Math.Cos(a) > 0 ? "start" : "end";
                string label = pl.Callback?.Invoke(_data.Labels[i], i) ?? _data.Labels[i];
                scene.Background.Add(new BitChartSvgText
                {
                    X = lx, Y = ly, Text = label, Fill = pl.Color,
                    FontSize = pl.Font.Size, FontFamily = pl.Font.Family, FontWeight = pl.Font.Weight,
                    Anchor = anchor, Baseline = "central"
                });
            }
        }

        // Radial tick labels (with optional backdrop).
        if (rOpts.Display && rOpts.Ticks.Display)
        {
            foreach (var t in rScale.Ticks)
            {
                double rr = t.Pixel;
                if (rr <= 0.01) continue;
                if (rOpts.ShowLabelBackdrop)
                {
                    double w = BitChartTextMeasure.Width(t.Label, rOpts.Ticks.Font.Size) + 4;
                    scene.Background.Add(new BitChartSvgRect { X = cx + 2, Y = cy - rr - rOpts.Ticks.Font.Size * 0.55, Width = w, Height = rOpts.Ticks.Font.Size + 2, Fill = rOpts.BackdropColor });
                }
                scene.Background.Add(new BitChartSvgText { X = cx + 4, Y = cy - rr, Text = t.Label, Fill = rOpts.Ticks.Color, FontSize = rOpts.Ticks.Font.Size, FontFamily = rOpts.Ticks.Font.Family, Anchor = "start", Baseline = "central" });
            }
        }

        // Datasets.
        for (int d = 0; d < _data.Datasets.Count; d++)
        {
            var ds = _data.Datasets[d];
            if (IsHidden(d, ds)) continue;
            string border = ResolveBorder(ds, d, 0, false);
            string fill = ds.FillColor ?? BitChartColorUtil.WithAlpha(border, 0.2);

            var verts = new List<(double x, double y, int di)>();
            for (int i = 0; i < n && i < ds.Data.Count; i++)
            {
                if (ds.Data[i] is not { } v) continue;   // skip null vertices
                double a = start + angleStep * i;
                double rr = rScale.PixelFor(v);
                verts.Add((cx + Math.Cos(a) * rr, cy + Math.Sin(a) * rr, i));
            }
            if (verts.Count == 0) continue;

            // Add the web (line + filled surface) to the animated series layer so it grows in
            // together with the joint points instead of staying fixed.
            scene.Series.Add(new BitChartSvgPolygon
            {
                Points = verts.Select(p => (p.x, p.y)).ToList(),
                Fill = ds.Fill != BitChartFillMode.None ? fill : "none",
                Stroke = border,
                StrokeWidth = ds.BorderWidth <= 1 ? 2 : ds.BorderWidth
            });

            foreach (var p in verts)
                AddPoint(scene, ds, d, p.di, p.x, p.y, ds.Data[p.di] ?? 0, Math.Max(3, ds.PointRadius), border);
        }
    }
}
