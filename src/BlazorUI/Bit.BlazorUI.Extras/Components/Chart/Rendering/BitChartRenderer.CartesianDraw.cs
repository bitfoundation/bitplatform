
namespace Bit.BlazorUI;

public sealed partial class BitChartRenderer
{
    private void DrawGrid(BitChartScene scene, BitChartArea plot, BitChartAxisScale indexScale,
        List<BitChartAxisScale> leftAxes, List<BitChartAxisScale> rightAxes, List<BitChartAxisScale> centerAxes)
    {
        bool firstValueGridDrawn = false;

        // Value axes. In a vertical chart they run down the sides and stack outwards by their width;
        // with a vertical index axis (horizontal bars) they run along the bottom/top and stack by height.
        double near = IsVertical ? plot.Left : plot.Bottom;
        foreach (var axis in leftAxes)
        {
            DrawValueAxis(scene, plot, axis, near, isRight: false, drawArea: !firstValueGridDrawn);
            firstValueGridDrawn = true;
            near += IsVertical ? -ReserveAxisWidth(axis) : ReserveAxisHeight(axis);
        }
        double far = IsVertical ? plot.Right : plot.Top;
        foreach (var axis in rightAxes)
        {
            DrawValueAxis(scene, plot, axis, far, isRight: true, drawArea: false);
            far += IsVertical ? ReserveAxisWidth(axis) : -ReserveAxisHeight(axis);
        }

        // Axes pinned to the other axis' zero line, drawn inside the plot.
        foreach (var axis in centerAxes)
            DrawValueAxis(scene, plot, axis, ZeroLineAlong(indexScale, plot), isRight: false, drawArea: false);

        // Index axis. It too can sit on the value axis' zero line rather than along the edge.
        double indexBaseline = PositionOf(indexScale.Options, IsVertical ? BitChartPosition.Bottom : BitChartPosition.Left) == BitChartPosition.Center
            ? ZeroLineAcross(leftAxes.Concat(rightAxes).Concat(centerAxes).FirstOrDefault(), plot)
            : IsVertical ? plot.Bottom : plot.Left;
        DrawIndexAxis(scene, plot, indexScale, indexBaseline);
    }

    /// <summary>The pixel along the index axis where its value is zero, clamped into the plot.</summary>
    private double ZeroLineAlong(BitChartAxisScale indexScale, BitChartArea plot)
    {
        double p = indexScale.Type == BitChartScaleType.Category
            ? indexScale.PixelForIndex(0, HasBars())
            : indexScale.PixelFor(0);
        return IsVertical ? Math.Clamp(p, plot.Left, plot.Right) : Math.Clamp(p, plot.Top, plot.Bottom);
    }

    /// <summary>The pixel across the plot where the given value axis reads zero, clamped into the plot.</summary>
    private double ZeroLineAcross(BitChartAxisScale? valueScale, BitChartArea plot)
    {
        double fallback = IsVertical ? plot.Bottom : plot.Left;
        if (valueScale is null) return fallback;
        double p = valueScale.PixelFor(0);
        return IsVertical ? Math.Clamp(p, plot.Top, plot.Bottom) : Math.Clamp(p, plot.Left, plot.Right);
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
                        X1 = axisPos, Y1 = y,
                        X2 = isRight ? axisPos + g.TickLength : axisPos - g.TickLength, Y2 = y,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                {
                    // Mirrored labels sit inside the plot area, on the other side of the axis line.
                    double lx = tk.Mirror
                        ? isRight ? axisPos - g.TickLength - tk.Padding : axisPos + g.TickLength + tk.Padding
                        : isRight ? axisPos + g.TickLength + tk.Padding : axisPos - g.TickLength - tk.Padding;
                    bool anchorStart = tk.Mirror ? !isRight : isRight;
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = lx, Y = y + TickLabelOffset(tk), Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = anchorStart ? "start" : "end", Baseline = "central"
                    });
                }
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
                int dir = isRight ? -1 : 1;   // "right" means the far edge, which is the top here
                if (g.DrawTicks)
                    scene.Background.Add(new BitChartSvgLine
                    {
                        X1 = x, Y1 = axisPos, X2 = x, Y2 = axisPos + dir * g.TickLength,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = x + TickLabelOffset(tk), Y = axisPos + dir * (g.TickLength + tk.Padding + tk.Font.Size * 0.5),
                        Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = TickAnchor(tk), Baseline = "central"
                    });
            }
        }

        if (o.Title.Display)
        {
            if (IsVertical)
                scene.Background.Add(new BitChartSvgText
                {
                    X = isRight ? axisPos + ReserveAxisWidth(axis) - o.Title.Font.Size : axisPos - ReserveAxisWidth(axis) + o.Title.Font.Size,
                    Y = plot.CenterY, Text = o.Title.Text, Fill = o.Title.Color,
                    FontFamily = o.Title.Font.Family, FontSize = o.Title.Font.Size, FontWeight = o.Title.Font.Weight,
                    Anchor = "middle", Baseline = "central", Rotation = isRight ? 90 : -90
                });
            else
                scene.Background.Add(new BitChartSvgText
                {
                    // A value axis under a horizontal-bar chart reserves height, not width.
                    X = plot.CenterX, Y = axisPos + ReserveAxisHeight(axis) - o.Title.Font.Size * 0.3,
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
                scene.Background.Add(new BitChartSvgLine { X1 = plot.Left, Y1 = axisPos, X2 = plot.Right, Y2 = axisPos, Stroke = b.Color, StrokeWidth = b.Width, Dash = BitChartSvg.Dash(b.Dash) });
        }
    }

    private void DrawIndexAxis(BitChartScene scene, BitChartArea plot, BitChartAxisScale axis, double baseline)
    {
        var o = axis.Options;
        if (!ScaleVisible(o)) return;
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
                        X1 = px, Y1 = baseline, X2 = px, Y2 = baseline + g.TickLength,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                {
                    double rot = axis.LabelRotation;
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = px + TickLabelOffset(tk), Y = baseline + g.TickLength + tk.Padding + (Math.Abs(rot) > 1e-3 ? tk.Font.Size * 0.35 : tk.Font.Size * 0.7),
                        Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = Math.Abs(rot) > 1e-3 ? "end" : TickAnchor(tk), Baseline = "auto", Rotation = rot
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
                        X1 = baseline, Y1 = px, X2 = baseline - g.TickLength, Y2 = px,
                        Stroke = g.TickColor, StrokeWidth = g.LineWidth
                    });
                if (tk.Display)
                    scene.Background.Add(new BitChartSvgText
                    {
                        X = tk.Mirror ? baseline + g.TickLength + tk.Padding : baseline - g.TickLength - tk.Padding,
                        Y = px + TickLabelOffset(tk), Text = tick.Label, Fill = tk.Color,
                        FontFamily = tk.Font.Family, FontSize = tk.Font.Size, FontWeight = tk.Font.Weight,
                        Anchor = tk.Mirror ? "start" : "end", Baseline = "central"
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
                scene.Background.Add(new BitChartSvgLine { X1 = plot.Left, Y1 = baseline, X2 = plot.Right, Y2 = baseline, Stroke = b.Color, StrokeWidth = b.Width, Dash = BitChartSvg.Dash(b.Dash) });
            else
                scene.Background.Add(new BitChartSvgLine { X1 = baseline, Y1 = plot.Top, X2 = baseline, Y2 = plot.Bottom, Stroke = b.Color, StrokeWidth = b.Width, Dash = BitChartSvg.Dash(b.Dash) });
        }
    }

    private bool HasBars() => _data.Datasets.Where((d, i) => !IsHidden(i, d)).Any(d => EffectiveType(d) == BitChartType.Bar);

    /// <summary>Text anchor for a tick label honoring <see cref="BitChartTickOptions.Align"/>.</summary>
    private static string TickAnchor(BitChartTickOptions tk) => tk.Align switch
    {
        BitChartAlign.Start => "start",
        BitChartAlign.End => "end",
        _ => "middle"
    };

    /// <summary>Extra pixel shift applied to a tick label along the axis.</summary>
    private static double TickLabelOffset(BitChartTickOptions tk) => tk.LabelOffset;

    /// <summary>Draws a secondary x-axis (ticks/labels/border/title) at a baseline outside the plot.</summary>
    private void DrawSecondaryXAxis(BitChartScene scene, BitChartArea plot, BitChartAxisScale axis, double baselineY, bool atBottom)
    {
        var o = axis.Options;
        if (!ScaleVisible(o)) return;
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
                    Anchor = "middle", Baseline = "auto"
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
