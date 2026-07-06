namespace Bit.BlazorUI;

/// <summary>A single annotation (line, box, point or label).</summary>
public sealed class BitChartAnnotation
{
    public BitChartAnnotationKind Kind { get; set; } = BitChartAnnotationKind.Line;

    // Line
    public BitChartLineOrientation Orientation { get; set; } = BitChartLineOrientation.Horizontal;
    /// <summary>Value on the relevant axis (y value for horizontal lines, x value/index for vertical).</summary>
    public double Value { get; set; }
    public string AxisId { get; set; } = "y";

    // Box / point bounds (value coordinates). Null = chart edge.
    public double? XMin { get; set; }
    public double? XMax { get; set; }
    public double? YMin { get; set; }
    public double? YMax { get; set; }
    /// <summary>For vertical line / box X bounds, interpret as a category index rather than a value.</summary>
    public bool XIsIndex { get; set; }

    public string Color { get; set; } = "#ff6384";
    public string? FillColor { get; set; }
    public double LineWidth { get; set; } = 2;
    public List<double>? Dash { get; set; }

    public string? Label { get; set; }
    public string LabelColor { get; set; } = "#fff";
    public string LabelBackground { get; set; } = "#ff6384";
    public bool DrawBehindDatasets { get; set; }
}
