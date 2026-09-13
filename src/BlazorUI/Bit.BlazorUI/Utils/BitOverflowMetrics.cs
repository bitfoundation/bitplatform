namespace Bit.BlazorUI;

/// <summary>
/// The room a single-line list of children needs against the room it has, as measured on the DOM side,
/// so a component can decide how many of them to keep in that line.
/// </summary>
public class BitOverflowMetrics
{
    /// <summary>
    /// The width of the container in pixels.
    /// </summary>
    public double Available { get; set; }

    /// <summary>
    /// The width the content of the container takes in pixels, which is larger than the available
    /// width while the content overflows.
    /// </summary>
    public double Content { get; set; }

    /// <summary>
    /// The widths of the measured children in DOM order, in pixels.
    /// </summary>
    public double[] Widths { get; set; } = [];
}
