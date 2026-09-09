
namespace Bit.BlazorUI;

/// <summary>
/// Draws line/box/point/label annotations over a cartesian chart, mirroring the popular
/// chartjs-plugin-annotation.
/// </summary>
public sealed class BitChartAnnotationPlugin : IBitChartPlugin
{
    public string Id => "annotation";
    public List<BitChartAnnotation> Annotations { get; } = new();

    public BitChartAnnotationPlugin() { }
    public BitChartAnnotationPlugin(params BitChartAnnotation[] annotations) => Annotations.AddRange(annotations);

    public void BeforeDatasetsDraw(BitChartPluginContext ctx)
    {
        foreach (var a in Annotations.Where(a => a.DrawBehindDatasets))
            Draw(ctx, a, behind: true);
    }

    public void AfterDatasetsDraw(BitChartPluginContext ctx)
    {
        foreach (var a in Annotations.Where(a => !a.DrawBehindDatasets))
            Draw(ctx, a, behind: false);
    }

    private void Draw(BitChartPluginContext ctx, BitChartAnnotation a, bool behind)
    {
        if (!ctx.IsCartesian || ctx.Plot is not { } plot) return;
        Action<BitChartSvgNode> add = behind ? ctx.AddBehind : ctx.AddFront;

        double X(double v, bool isIndex) => isIndex ? ctx.XForIndex((int)v) : ctx.XForValue(v);

        switch (a.Kind)
        {
            case BitChartAnnotationKind.Line:
                if (a.Orientation == BitChartLineOrientation.Horizontal)
                {
                    double y = ctx.YForValue(a.Value, a.AxisId);
                    add(new BitChartSvgLine { X1 = plot.Left, Y1 = y, X2 = plot.Right, Y2 = y, Stroke = a.Color, StrokeWidth = a.LineWidth, Dash = BitChartSvg.Dash(a.Dash) });
                    if (!string.IsNullOrEmpty(a.Label))
                        AddLabel(add, a, plot.Right - 4, y, "end");
                }
                else
                {
                    double x = X(a.Value, a.XIsIndex);
                    add(new BitChartSvgLine { X1 = x, Y1 = plot.Top, X2 = x, Y2 = plot.Bottom, Stroke = a.Color, StrokeWidth = a.LineWidth, Dash = BitChartSvg.Dash(a.Dash) });
                    if (!string.IsNullOrEmpty(a.Label))
                        AddLabel(add, a, x, plot.Top + 10, "middle");
                }
                break;

            case BitChartAnnotationKind.Box:
            {
                double x1 = a.XMin is { } xm ? X(xm, a.XIsIndex) : plot.Left;
                double x2 = a.XMax is { } xM ? X(xM, a.XIsIndex) : plot.Right;
                double y1 = a.YMax is { } yM ? ctx.YForValue(yM, a.AxisId) : plot.Top;
                double y2 = a.YMin is { } ym ? ctx.YForValue(ym, a.AxisId) : plot.Bottom;

                // Clamp to the plot area so a box whose bounds fall outside the visible
                // axis range (e.g. YMin below the axis minimum) doesn't spill past the chart.
                x1 = Math.Clamp(x1, plot.Left, plot.Right);
                x2 = Math.Clamp(x2, plot.Left, plot.Right);
                y1 = Math.Clamp(y1, plot.Top, plot.Bottom);
                y2 = Math.Clamp(y2, plot.Top, plot.Bottom);

                add(new BitChartSvgRect
                {
                    X = Math.Min(x1, x2), Y = Math.Min(y1, y2),
                    Width = Math.Abs(x2 - x1), Height = Math.Abs(y2 - y1),
                    Fill = a.FillColor ?? BitChartColorUtil.WithAlpha(a.Color, 0.15),
                    Stroke = a.Color, StrokeWidth = a.LineWidth
                });
                if (!string.IsNullOrEmpty(a.Label))
                    AddLabel(add, a, (x1 + x2) / 2, (y1 + y2) / 2, "middle");
                break;
            }

            case BitChartAnnotationKind.Point:
            {
                double x = a.XMin is { } xv ? X(xv, a.XIsIndex) : plot.CenterX;
                double y = ctx.YForValue(a.Value, a.AxisId);
                add(new BitChartSvgCircle
                {
                    Cx = x, Cy = y, R = a.Radius ?? a.LineWidth * 2 + 2,
                    Fill = a.FillColor ?? a.Color, Stroke = a.Color, StrokeWidth = a.LineWidth
                });
                if (!string.IsNullOrEmpty(a.Label))
                    AddLabel(add, a, x, y - (a.Radius ?? a.LineWidth * 2 + 2) - a.LabelFont.LineHeightPx, "middle");
                break;
            }

            case BitChartAnnotationKind.Ellipse:
            {
                double x1 = a.XMin is { } xm ? X(xm, a.XIsIndex) : plot.Left;
                double x2 = a.XMax is { } xM ? X(xM, a.XIsIndex) : plot.Right;
                double y1 = a.YMax is { } yM ? ctx.YForValue(yM, a.AxisId) : plot.Top;
                double y2 = a.YMin is { } ym ? ctx.YForValue(ym, a.AxisId) : plot.Bottom;

                // Same clamp as the box: bounds outside the visible axis range must not spill past the chart.
                x1 = Math.Clamp(x1, plot.Left, plot.Right);
                x2 = Math.Clamp(x2, plot.Left, plot.Right);
                y1 = Math.Clamp(y1, plot.Top, plot.Bottom);
                y2 = Math.Clamp(y2, plot.Top, plot.Bottom);

                double cx = (x1 + x2) / 2, cy = (y1 + y2) / 2;
                double rx = Math.Abs(x2 - x1) / 2, ry = Math.Abs(y2 - y1) / 2;
                if (rx <= 0 || ry <= 0) break;

                // Two half-turn arcs make a closed ellipse without needing a primitive of its own.
                add(new BitChartSvgPath
                {
                    D = $"M {BitChartSvg.N(cx - rx)} {BitChartSvg.N(cy)} " +
                        $"A {BitChartSvg.N(rx)} {BitChartSvg.N(ry)} 0 1 0 {BitChartSvg.N(cx + rx)} {BitChartSvg.N(cy)} " +
                        $"A {BitChartSvg.N(rx)} {BitChartSvg.N(ry)} 0 1 0 {BitChartSvg.N(cx - rx)} {BitChartSvg.N(cy)} Z",
                    Fill = a.FillColor ?? BitChartColorUtil.WithAlpha(a.Color, 0.15),
                    Stroke = a.Color, StrokeWidth = a.LineWidth,
                    Dash = BitChartSvg.Dash(a.Dash)
                });
                if (!string.IsNullOrEmpty(a.Label))
                    AddLabel(add, a, cx, cy, "middle");
                break;
            }

            case BitChartAnnotationKind.Polygon:
            {
                double cx = a.XMin is { } xv ? X(xv, a.XIsIndex) : plot.CenterX;
                double cy = ctx.YForValue(a.Value, a.AxisId);
                double r = a.Radius ?? 12;
                int sides = Math.Max(3, a.Sides);
                if (r <= 0) break;

                var poly = new BitChartSvgPolygon
                {
                    Fill = a.FillColor ?? BitChartColorUtil.WithAlpha(a.Color, 0.2),
                    Stroke = a.Color, StrokeWidth = a.LineWidth
                };
                // Starts at the top so a triangle points up, which is what a reader expects of one.
                double start = -Math.PI / 2 + a.Rotation * Math.PI / 180;
                for (int i = 0; i < sides; i++)
                {
                    double angle = start + 2 * Math.PI * i / sides;
                    poly.Points.Add((cx + Math.Cos(angle) * r, cy + Math.Sin(angle) * r));
                }
                add(poly);
                if (!string.IsNullOrEmpty(a.Label))
                    AddLabel(add, a, cx, cy - r - a.LabelFont.LineHeightPx, "middle");
                break;
            }

            case BitChartAnnotationKind.Label:
            {
                double x = a.XMin is { } xv ? X(xv, a.XIsIndex) : plot.CenterX;
                double y = ctx.YForValue(a.Value, a.AxisId);
                AddLabel(add, a, x, y, "middle");
                break;
            }
        }
    }

    private static void AddLabel(Action<BitChartSvgNode> add, BitChartAnnotation a, double x, double y, string anchor)
    {
        // Measured rather than guessed from the character count, so the pill fits the text at any length.
        double fontSize = a.LabelFont.Size;
        double w = BitChartTextMeasure.Width(a.Label, fontSize, a.LabelFont.Weight) + 12;
        double h = a.LabelFont.LineHeightPx + 6;
        double left = anchor == "end" ? x - w : anchor == "middle" ? x - w / 2 : x;
        add(new BitChartSvgRect { X = left, Y = y - h / 2, Width = w, Height = h, Rx = 4, Fill = a.LabelBackground });
        add(new BitChartSvgText
        {
            X = left + w / 2, Y = y, Text = a.Label!, Fill = a.LabelColor,
            FontFamily = a.LabelFont.Family, FontSize = fontSize, FontWeight = a.LabelFont.Weight,
            Anchor = "middle", Baseline = "central"
        });
    }
}
