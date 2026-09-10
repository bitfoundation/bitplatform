namespace Bit.BlazorUI;

/// <summary>The shape an annotation draws, mirroring chartjs-plugin-annotation's annotation types.</summary>
public enum BitChartAnnotationKind
{
    /// <summary>A horizontal or vertical rule across the plot - a threshold, a target, a marker date.</summary>
    Line,

    /// <summary>A rectangle bounded by XMin/XMax and YMin/YMax - a highlighted region.</summary>
    Box,

    /// <summary>A dot at one coordinate.</summary>
    Point,

    /// <summary>Text in a pill at one coordinate, with no shape of its own.</summary>
    Label,

    /// <summary>An ellipse inscribed in the XMin/XMax and YMin/YMax bounds, for circling a region.</summary>
    Ellipse,

    /// <summary>A regular polygon of <see cref="BitChartAnnotation.Sides"/> sides, centered on the
    /// annotation's coordinate with a pixel <see cref="BitChartAnnotation.Radius"/>.</summary>
    Polygon
}
