namespace Bit.BlazorUI;

/// <summary>
/// Stroke and fill style for vector layers (polyline, polygon, circle, rectangle, GeoJSON).
/// </summary>
public sealed class BitMapVectorPathStyle
{
    /// <summary>Stroke color.</summary>
    public string Color { get; set; } = "#3388ff";

    /// <summary>Stroke width in pixels.</summary>
    public double Weight { get; set; } = 3;

    /// <summary>Stroke opacity (0–1).</summary>
    public double Opacity { get; set; } = 1;

    /// <summary>Fill color (defaults to <see cref="Color"/> when null).</summary>
    public string? FillColor { get; set; }

    /// <summary>Fill opacity (0–1).</summary>
    public double FillOpacity { get; set; } = 0.2;

    /// <summary>Stroke dash pattern (e.g. "5,10").</summary>
    public string? DashArray { get; set; }
}
