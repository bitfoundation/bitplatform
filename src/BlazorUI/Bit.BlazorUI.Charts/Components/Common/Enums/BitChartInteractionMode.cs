namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/general/interactions/modes.html">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartInteractionMode : BitChartStringEnum
{
    /// <summary>
    /// Finds all of the items that intersect the point.
    /// </summary>
    public static BitChartInteractionMode Point => new BitChartInteractionMode("point");

    /// <summary>
    /// Gets the items that are at the nearest distance to the point.
    /// The nearest item is determined based on the distance to the center of the chart item (point, bar).
    /// </summary>
    public static BitChartInteractionMode Nearest => new BitChartInteractionMode("nearest");

    /// <summary>
    /// Finds item at the same index.
    /// </summary>
    public static BitChartInteractionMode Index => new BitChartInteractionMode("index");

    /// <summary>
    /// Finds items in the same dataset.
    /// </summary>
    public static BitChartInteractionMode Dataset => new BitChartInteractionMode("dataset");

    /// <summary>
    /// Returns all items that would intersect based on the X coordinate of the position only.
    /// Would be useful for a vertical cursor implementation.
    /// <para>Note that this only applies to cartesian charts.</para>
    /// </summary>
    public static BitChartInteractionMode X => new BitChartInteractionMode("x");

    /// <summary>
    /// Returns all items that would intersect based on the Y coordinate of the position. This would be useful for a horizontal cursor implementation
    /// <para>Note that this only applies to cartesian charts.</para>
    /// </summary>
    public static BitChartInteractionMode Y => new BitChartInteractionMode("y");

    private BitChartInteractionMode(string stringRep) : base(stringRep) { }
}
