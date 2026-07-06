
namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private void DrawGrid(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale,
        Dictionary<string, BitChartAxisScale> valueScales, List<BitChartAxisScale> leftAxes, List<BitChartAxisScale> rightAxes)
    {
        bool firstValueGridDrawn = false;

        // Value axes.
        double leftX = plot.Left;
        foreach (var axis in leftAxes)
        {
            DrawValueAxis(scene, plot, axis, leftX, isRight: false, drawArea: !firstValueGridDrawn);
            firstValueGridDrawn = true;
            leftX -= ReserveValueAxis(axis);
        }
        double rightX = plot.Right;
        foreach (var axis in rightAxes)
        {
            DrawValueAxis(scene, plot, axis, rightX, isRight: true, drawArea: false);
            rightX += ReserveValueAxis(axis);
        }

        // Index axis.
        DrawIndexAxis(scene, plot, indexScale);
    }

    private void DrawValueAxis(BitChartScene scene, BitChartArea plot, BitChartAxisScale axis, double axisPos, bool isRight, bool drawArea)
    {
        var o = axis.Options;
        var g = o.Grid;
        var tk = o.Ticks;

        foreach (var tick in axis.Ticks)
        {
            if (IsVertical)
            {
                double y = tick.Pixel;
                if (y < plot.Top - 0.5 || y > plot.Bottom + 0.5) continue;
                if (tick.Minor)
                {
                    if (g.Display && drawArea && g.DrawOnChartArea)
                        scene.Background.Add(new BitChartSvgLine { X1 = plot.Left, Y1 = y, X2 = plot.Right, Y2 = y, Stroke = BitChartColorUtil.WithAlpha(g.Color, 0.4), StrokeWidth = g.LineWidth });
                    continue;
                }
                if (g.Display && drawArea && g.DrawOnChartArea)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = plot.Left, Y1 = y, X2 = plot.Right, Y2 = y,
                        Stroke = Math.Abs(tick.Value) < 1e-9 ? (g.ZeroLineColor ?? g.Color) : g.Color,
                        StrokeWidth = g.LineWidth, Dash = BitChartSvg.Dash(g.BorderDash)
                    });
                if (g.DrawTicks)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = isRight ? axisPos : axisPos, Y1 = y,
                        X2 = isRight ? axisPos + g.TickLength : axisPos - g.TickLength, Y2 = y,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = isRight ? axisPos + g.TickLength + tk.Padding : axisPos - g.TickLength - tk.Padding,
                        Y = y, Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = isRight ? "start" : "end", Baseline = "central"
                    });
            }
            else
            {
                double x = tick.Pixel;
                if (x < plot.Left - 0.5 || x > plot.Right + 0.5) continue;
                if (tick.Minor)
                {
                    if (g.Display && drawArea && g.DrawOnChartArea)
                        scene.Background.Add(new BitChartSvgLine { X1 = x, Y1 = plot.Top, X2 = x, Y2 = plot.Bottom, Stroke = BitChartColorUtil.WithAlpha(g.Color, 0.4), StrokeWidth = g.LineWidth });
                    continue;
                }
                if (g.Display && drawArea && g.DrawOnChartArea)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = x, Y1 = plot.Top, X2 = x, Y2 = plot.Bottom,
                        Stroke = Math.Abs(tick.Value) < 1e-9 ? (g.ZeroLineColor ?? g.Color) : g.Color,
                        StrokeWidth = g.LineWidth, Dash = BitChartSvg.Dash(g.BorderDash)
                    });
                if (g.DrawTicks)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = x, Y1 = plot.Bottom, X2 = x, Y2 = plot.Bottom + g.TickLength,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = x, Y = plot.Bottom + g.TickLength + tk.Padding + tk.Font.Size * 0.5,
                        Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = "middle", Baseline = "central"
                    });
            }
        }

        if (o.Title.Display)
        {
            if (IsVertical)
                scene.Background.Add(new BitChartSvgText
                {
                    X = isRight ? axisPos + ReserveValueAxis(axis) - o.Title.Font.Size : axisPos - ReserveValueAxis(axis) + o.Title.Font.Size,
                    Y = plot.CenterY, Text = o.Title.Text, Fill = o.Title.Color,
                    FontFamily = o.Title.Font.Family, FontSize = o.Title.Font.Size, FontWeight = o.Title.Font.Weight,
                    Anchor = "middle", Baseline = "central", Rotation = isRight ? 90 : -90
                });
            else
                scene.Background.Add(new BitChartSvgText
                {
                    X = plot.CenterX, Y = plot.Bottom + ReserveValueAxis(axis) - o.Title.Font.Size * 0.3,
                    Text = o.Title.Text, Fill = o.Title.Color,
                    FontFamily = o.Title.Font.Family, FontSize = o.Title.Font.Size, FontWeight = o.Title.Font.Weight,
                    Anchor = "middle", Baseline = "central"
                });
        }

        // Axis border line.
        if (o.Border.Display)
        {
            var b = o.Border;
            if (IsVertical)
                scene.Background.Add(new BitChartSvgLine { X1 = axisPos, Y1 = plot.Top, X2 = axisPos, Y2 = plot.Bottom, Stroke = b.Color, StrokeWidth = b.Width, Dash = BitChartSvg.Dash(b.Dash) });
            else
                scene.Background.Add(new BitChartSvgLine { X1 = plot.Left, Y1 = plot.Bottom, X2 = plot.Right, Y2 = plot.Bottom, Stroke = b.Color, StrokeWidth = b.Width, Dash = BitChartSvg.Dash(b.Dash) });
        }
    }

    private void DrawIndexAxis(BitChartScene scene, BitChartArea plot, BitChartAxisScale axis)
    {
        var o = axis.Options;
        if (!o.Display) return;
        var g = o.Grid;
        var tk = o.Ticks;
        bool centered = HasBars();

        foreach (var tick in axis.Ticks)
        {
            double px = axis.Type == BitChartScaleType.Category
                ? axis.PixelForIndex((int)tick.Value, centered)
                : tick.Pixel;

            if (IsVertical)
            {
                if (g.Display && g.DrawOnChartArea)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = px, Y1 = plot.Top, X2 = px, Y2 = plot.Bottom,
                        Stroke = g.Color, StrokeWidth = g.LineWidth, Dash = BitChartSvg.Dash(g.BorderDash)
                    });
                if (g.DrawTicks)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = px, Y1 = plot.Bottom, X2 = px, Y2 = plot.Bottom + g.TickLength,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                {
                    double rot = axis.LabelRotation;
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = px, Y = plot.Bottom + g.TickLength + tk.Padding + (Math.Abs(rot) > 1e-3 ? tk.Font.Size * 0.35 : tk.Font.Size * 0.7),
                        Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = Math.Abs(rot) > 1e-3 ? "end" : "middle", Baseline = "auto", Rotation = rot
                    });
                }
            }
            else
            {
                if (g.Display && g.DrawOnChartArea)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = plot.Left, Y1 = px, X2 = plot.Right, Y2 = px,
                        Stroke = g.Color, StrokeWidth = g.LineWidth, Dash = BitChartSvg.Dash(g.BorderDash)
                    });
                if (g.DrawTicks)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = plot.Left, Y1 = px, X2 = plot.Left - g.TickLength, Y2 = px,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = plot.Left - g.TickLength - tk.Padding, Y = px, Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = "end", Baseline = "central"
                    });
            }
        }

        if (o.Title.Display)
        {
            if (IsVertical)
                scene.Background.Add(new BitChartSvgText
                {
                    X = plot.CenterX, Y = _h - _options.Layout.Padding.Bottom - o.Title.Font.Size * 0.2,
                    Text = o.Title.Text, Fill = o.Title.Color,
                    FontFamily = o.Title.Font.Family, FontSize = o.Title.Font.Size, FontWeight = o.Title.Font.Weight,
                    Anchor = "middle", Baseline = "auto"
                });
            else
                scene.Background.Add(new BitChartSvgText
                {
                    X = _options.Layout.Padding.Left + o.Title.Font.Size, Y = plot.CenterY,
                    Text = o.Title.Text, Fill = o.Title.Color,
                    FontFamily = o.Title.Font.Family, FontSize = o.Title.Font.Size, FontWeight = o.Title.Font.Weight,
                    Anchor = "middle", Baseline = "central", Rotation = -90
                });
        }

        // Axis border line.
        if (o.Border.Display)
        {
            var b = o.Border;
            if (IsVertical)
                scene.Background.Add(new BitChartSvgLine { X1 = plot.Left, Y1 = plot.Bottom, X2 = plot.Right, Y2 = plot.Bottom, Stroke = b.Color, StrokeWidth = b.Width, Dash = BitChartSvg.Dash(b.Dash) });
            else
                scene.Background.Add(new BitChartSvgLine { X1 = plot.Left, Y1 = plot.Top, X2 = plot.Left, Y2 = plot.Bottom, Stroke = b.Color, StrokeWidth = b.Width, Dash = BitChartSvg.Dash(b.Dash) });
        }
    }

    private bool HasBars() => _data.Datasets.Where((d, i) => !IsHidden(i, d)).Any(d => EffectiveType(d) == BitChartType.Bar);

    /// <summary>Draws a secondary x-axis (ticks/labels/border/title) at a baseline outside the plot.</summary>
    private void DrawSecondaryXAxis(BitChartScene scene, BitChartArea plot, BitChartAxisScale axis, double baselineY, bool atBottom)
    {
        var o = axis.Options;
        if (!o.Display) return;
        var g = o.Grid;
        var tk = o.Ticks;
        int dir = atBottom ? 1 : -1;

        foreach (var tick in axis.Ticks)
        {
            double px = tick.Pixel;
            if (px < plot.Left - 0.5 || px > plot.Right + 0.5) continue;

            if (g.Display && g.DrawOnChartArea && !tick.Minor)
                scene.Background.Add(new BitChartSvgLine { X1 = px, Y1 = plot.Top, X2 = px, Y2 = plot.Bottom, Stroke = g.Color, StrokeWidth = g.LineWidth, Dash = BitChartSvg.Dash(g.BorderDash) });
            if (g.DrawTicks)
                scene.Background.Add(new BitChartSvgLine { X1 = px, Y1 = baselineY, X2 = px, Y2 = baselineY + dir * g.TickLength, Stroke = g.TickColor, StrokeWidth = g.LineWidth });
            if (tk.Display && !tick.Minor)
                scene.Background.Add(new BitChartSvgText
                {
                    X = px, Y = baselineY + dir * (g.TickLength + tk.Padding + tk.Font.Size * (atBottom ? 0.7 : 0.1)),
                    Text = tick.Label, Fill = tk.Color,
                    FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                    Anchor = "middle", Baseline = atBottom ? "auto" : "auto"
                });
        }

        if (o.Border.Display)
            scene.Background.Add(new BitChartSvgLine { X1 = plot.Left, Y1 = baselineY, X2 = plot.Right, Y2 = baselineY, Stroke = o.Border.Color, StrokeWidth = o.Border.Width, Dash = BitChartSvg.Dash(o.Border.Dash) });

        if (o.Title.Display)
            scene.Background.Add(new BitChartSvgText
            {
                X = plot.CenterX, Y = baselineY + dir * (g.TickLength + tk.Padding + tk.Font.LineHeightPx + o.Title.Font.Size * 0.6),
                Text = o.Title.Text, Fill = o.Title.Color,
                FontFamily = o.Title.Font.Family, FontSize = o.Title.Font.Size, FontWeight = o.Title.Font.Weight,
                Anchor = "middle", Baseline = "central"
            });
    }
}
