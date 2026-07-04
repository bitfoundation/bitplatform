
namespace Bit.BlazorUI;

/// <summary>Builds SVG marker shapes for the Chart.js point styles.</summary>
public static class BitChartPointShapes
{
    public static BitChartSvgNode? Build(BitChartPointStyle style, double x, double y, double r,
        string fill, string stroke, double strokeWidth)
    {
        switch (style)
        {
            case BitChartPointStyle.None:
                return null;
            case BitChartPointStyle.Rect:
                return new BitChartSvgRect { X = x - r, Y = y - r, Width = r * 2, Height = r * 2, Fill = fill, Stroke = stroke, StrokeWidth = strokeWidth };
            case BitChartPointStyle.RectRounded:
                return new BitChartSvgRect { X = x - r, Y = y - r, Width = r * 2, Height = r * 2, Rx = r * 0.4, Fill = fill, Stroke = stroke, StrokeWidth = strokeWidth };
            case BitChartPointStyle.Triangle:
                return new BitChartSvgPolygon
                {
                    Points = { (x, y - r), (x - r, y + r), (x + r, y + r) },
                    Fill = fill, Stroke = stroke, StrokeWidth = strokeWidth
                };
            case BitChartPointStyle.RectRot:
                return new BitChartSvgPolygon
                {
                    Points = { (x, y - r), (x + r, y), (x, y + r), (x - r, y) },
                    Fill = fill, Stroke = stroke, StrokeWidth = strokeWidth
                };
            case BitChartPointStyle.Cross:
                return new BitChartSvgPath { D = $"M {BitChartSvg.N(x)} {BitChartSvg.N(y - r)} L {BitChartSvg.N(x)} {BitChartSvg.N(y + r)} M {BitChartSvg.N(x - r)} {BitChartSvg.N(y)} L {BitChartSvg.N(x + r)} {BitChartSvg.N(y)}", Stroke = stroke, StrokeWidth = Math.Max(1, strokeWidth) };
            case BitChartPointStyle.CrossRot:
            {
                double o = r * 0.707;
                return new BitChartSvgPath { D = $"M {BitChartSvg.N(x - o)} {BitChartSvg.N(y - o)} L {BitChartSvg.N(x + o)} {BitChartSvg.N(y + o)} M {BitChartSvg.N(x - o)} {BitChartSvg.N(y + o)} L {BitChartSvg.N(x + o)} {BitChartSvg.N(y - o)}", Stroke = stroke, StrokeWidth = Math.Max(1, strokeWidth) };
            }
            case BitChartPointStyle.Dash:
            case BitChartPointStyle.Line:
                return new BitChartSvgPath { D = $"M {BitChartSvg.N(x - r)} {BitChartSvg.N(y)} L {BitChartSvg.N(x + r)} {BitChartSvg.N(y)}", Stroke = stroke, StrokeWidth = Math.Max(2, strokeWidth) };
            case BitChartPointStyle.Star:
                return BuildStar(x, y, r, fill, stroke, strokeWidth);
            default:
                return new BitChartSvgCircle { Cx = x, Cy = y, R = r, Fill = fill, Stroke = stroke, StrokeWidth = strokeWidth };
        }
    }

    private static BitChartSvgPolygon BuildStar(double cx, double cy, double r, string fill, string stroke, double sw)
    {
        var poly = new BitChartSvgPolygon { Fill = fill, Stroke = stroke, StrokeWidth = sw };
        for (int i = 0; i < 10; i++)
        {
            double rad = i % 2 == 0 ? r : r / 2;
            double ang = Math.PI / 5 * i - Math.PI / 2;
            poly.Points.Add((cx + Math.Cos(ang) * rad, cy + Math.Sin(ang) * rad));
        }
        return poly;
    }
}
