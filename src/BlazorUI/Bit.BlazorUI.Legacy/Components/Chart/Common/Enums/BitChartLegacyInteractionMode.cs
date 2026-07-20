namespace Bit.BlazorUI.Legacy;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/general/interactions/modes.html">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartLegacyInteractionMode : BitChartLegacyStringEnum
{
    /// <summary>
    /// Finds all of the items that intersect the point.
    /// </summary>
    public static BitChartLegacyInteractionMode Point => new BitChartLegacyInteractionMode("point");

    /// <summary>
    /// Gets the items that are at the nearest distance to the point.
    /// The nearest item is determined based on the distance to the center of the chart item (point, bar).
    /// </summary>
    public static BitChartLegacyInteractionMode Nearest => new BitChartLegacyInteractionMode("nearest");

    /// <summary>
    /// Finds item at the same index.
    /// </summary>
    public static BitChartLegacyInteractionMode Index => new BitChartLegacyInteractionMode("index");

    /// <summary>
    /// Finds items in the same dataset.
    /// </summary>
    public static BitChartLegacyInteractionMode Dataset => new BitChartLegacyInteractionMode("dataset");

    /// <summary>
    /// Returns all items that would intersect based on the X coordinate of the position only.
    /// Would be useful for a vertical cursor implementation.
    /// <para>Note that this only applies to cartesian charts.</para>
    /// </summary>
    public static BitChartLegacyInteractionMode X => new BitChartLegacyInteractionMode("x");

    /// <summary>
    /// Returns all items that would intersect based on the Y coordinate of the position. This would be useful for a horizontal cursor implementation
    /// <para>Note that this only applies to cartesian charts.</para>
    /// </summary>
    public static BitChartLegacyInteractionMode Y => new BitChartLegacyInteractionMode("y");

    private BitChartLegacyInteractionMode(string stringRep) : base(stringRep) { }
}
