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

    /// <summary>
    /// Line and outline color, from which a box, ellipse or polygon derives its translucent fill and the label its pill.
    /// Defaults to the public <c>--bit-Chart-annotation-color</c> variable, falling back to the theme's secondary
    /// foreground: an annotation is a reference mark, not data, so it reads as chrome until it is given a color.
    /// </summary>
    public string Color { get; set; } = "var(--bit-Chart-annotation-color, var(--bit-clr-fg-sec))";
    public string? FillColor { get; set; }
    public double LineWidth { get; set; } = 2;
    public List<double>? Dash { get; set; }

    /// <summary>Number of sides of a <see cref="BitChartAnnotationKind.Polygon"/> (3 = triangle).</summary>
    public int Sides { get; set; } = 3;

    /// <summary>Radius in pixels of a <see cref="BitChartAnnotationKind.Polygon"/> or a
    /// <see cref="BitChartAnnotationKind.Point"/>. When null a point sizes itself from its line width.</summary>
    public double? Radius { get; set; }

    /// <summary>Rotation in degrees of a <see cref="BitChartAnnotationKind.Polygon"/>.</summary>
    public double Rotation { get; set; }

    /// <summary>Text drawn in a pill beside the shape. It is also what a screen reader is told about the annotation.</summary>
    public string? Label { get; set; }

    /// <summary>
    /// What a screen reader is told about the annotation, in place of the one written from its <see cref="Label"/>
    /// and value. The drawing itself is hidden from assistive technologies, so this is how a reader learns that a
    /// target line or a highlighted region is there.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Text color of the label. Defaults to the public <c>--bit-Chart-annotation-label-color</c> variable, falling back
    /// to the theme's primary background, which reads on the default annotation color in a light and a dark theme.
    /// </summary>
    public string LabelColor { get; set; } = "var(--bit-Chart-annotation-label-color, var(--bit-clr-bg-pri))";

    /// <summary>Pill color behind the label. When null it follows <see cref="Color"/>.</summary>
    public string? LabelBackground { get; set; }
    /// <summary>Font of the annotation label.</summary>
    public BitChartFont LabelFont { get; set; } = new() { Size = 11, Weight = "bold" };
    public bool DrawBehindDatasets { get; set; }
}
