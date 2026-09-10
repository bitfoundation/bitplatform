namespace Bit.BlazorUI;

/// <summary>Shape of a stroke's end caps.</summary>
public enum BitMapLineCap
{
    /// <summary>Rounded ends that extend past the endpoint by half the stroke width.</summary>
    Round,

    /// <summary>Squared off exactly at the endpoint.</summary>
    Butt,

    /// <summary>Squared off, extending past the endpoint by half the stroke width.</summary>
    Square,
}

/// <summary>Shape of the join between two stroke segments.</summary>
public enum BitMapLineJoin
{
    /// <summary>Rounded corner.</summary>
    Round,

    /// <summary>Flattened corner.</summary>
    Bevel,

    /// <summary>Pointed corner.</summary>
    Miter,
}

/// <summary>
/// Stroke and fill style for vector layers (polyline, polygon, circle, rectangle, GeoJSON).
/// </summary>
public sealed class BitMapVectorPathStyle
{
    /// <summary>Stroke color.</summary>
    public string Color { get; set; } = "#3388ff";

    /// <summary>Stroke width in pixels. Negative or non-finite (NaN/±Infinity) inputs are clamped to 0.</summary>
    public double Weight
    {
        get => _weight;
        set => _weight = double.IsFinite(value) && value > 0 ? value : 0;
    }
    private double _weight = 3;

    /// <summary>Stroke opacity (0–1). Non-finite (NaN/±Infinity) inputs default to 0; out-of-range values are clamped.</summary>
    public double Opacity
    {
        get => _opacity;
        set => _opacity = double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
    }
    private double _opacity = 1;

    /// <summary>
    /// Whether the shape is filled at all. Set it to false for an outline-only polygon; the
    /// alternative - a zero <see cref="FillOpacity"/> - still renders (and hit-tests) an
    /// invisible fill.
    /// <para><b>Provider support:</b> Leaflet and OpenLayers. Ignored elsewhere.</para>
    /// </summary>
    public bool Fill { get; set; } = true;

    /// <summary>Fill color (defaults to <see cref="Color"/> when null).</summary>
    public string? FillColor { get; set; }

    /// <summary>Fill opacity (0–1). Non-finite (NaN/±Infinity) inputs default to 0; out-of-range values are clamped.</summary>
    public double FillOpacity
    {
        get => _fillOpacity;
        set => _fillOpacity = double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
    }
    private double _fillOpacity = 0.2;

    /// <summary>
    /// Stroke dash pattern - a list of dash and gap lengths, e.g. <c>"5,10"</c> or <c>"5 10"</c>.
    /// Both comma and whitespace separators are accepted.
    /// </summary>
    public string? DashArray { get; set; }

    /// <summary>
    /// Distance into the dash pattern at which the stroke starts. Use it to align the dashes of
    /// two overlapping lines, or to animate a "marching ants" effect by stepping the value.
    /// <para><b>Provider support:</b> Leaflet only (SVG <c>stroke-dashoffset</c>). Ignored elsewhere.</para>
    /// </summary>
    public string? DashOffset { get; set; }

    /// <summary>
    /// Shape of the stroke's end caps.
    /// <para><b>Provider support:</b> Leaflet and OpenLayers; MapLibre and Mapbox honour it on line layers.</para>
    /// </summary>
    public BitMapLineCap LineCap { get; set; } = BitMapLineCap.Round;

    /// <summary>
    /// Shape of the join between two stroke segments.
    /// <para><b>Provider support:</b> Leaflet and OpenLayers; MapLibre and Mapbox honour it on line layers.</para>
    /// </summary>
    public BitMapLineJoin LineJoin { get; set; } = BitMapLineJoin.Round;
}
