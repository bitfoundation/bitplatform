namespace Bit.BlazorUI;

/// <summary>
/// A radial gradient fill. Center and radius are expressed as fractions (0..1) of the filled
/// shape's bounding box, mirroring SVG <c>objectBoundingBox</c> units.
/// </summary>
public sealed class BitChartRadialGradient : BitChartGradientBase
{
    /// <summary>Center X as a fraction of the bounding box (0..1).</summary>
    public double CenterX { get; set; } = 0.5;
    /// <summary>Center Y as a fraction of the bounding box (0..1).</summary>
    public double CenterY { get; set; } = 0.5;
    /// <summary>Radius as a fraction of the bounding box (0..1).</summary>
    public double Radius { get; set; } = 0.5;

    public BitChartRadialGradient() { }

    public BitChartRadialGradient(params BitChartGradientStop[] stops) => Stops.AddRange(stops);

    /// <summary>Convenience: a two-stop center→edge radial gradient.</summary>
    public static BitChartRadialGradient Center2(string center, string edge) =>
        new(new BitChartGradientStop(0, center), new BitChartGradientStop(1, edge));
}
