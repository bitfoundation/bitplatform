using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>Helpers for formatting numbers in an invariant, SVG-friendly way.</summary>
public static class BitChartSvg
{
    public static string N(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return "0";
        return Math.Round(v, 3).ToString(CultureInfo.InvariantCulture);
    }

    public static string Dash(IEnumerable<double>? dash) =>
        dash is null ? "" : string.Join(",", dash.Select(N));
}
