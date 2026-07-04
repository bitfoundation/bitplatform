namespace Bit.BlazorUI;

/// <summary>Grid line configuration for a scale.</summary>
public sealed class BitChartGridOptions
{
    public bool Display { get; set; } = true;
    public string Color { get; set; } = "var(--bit-clr-brd-sec, rgba(0,0,0,0.1))";
    public double LineWidth { get; set; } = 1;
    public bool DrawOnChartArea { get; set; } = true;
    public bool DrawTicks { get; set; } = true;
    public double TickLength { get; set; } = 6;
    public string TickColor { get; set; } = "var(--bit-clr-brd-sec, rgba(0,0,0,0.1))";
    public List<double>? BorderDash { get; set; }
    /// <summary>Color of the zero line; when null the normal grid color is used.</summary>
    public string? ZeroLineColor { get; set; }
    /// <summary>Radial scales: draw grid as concentric circles instead of polygons.</summary>
    public bool Circular { get; set; }
}
