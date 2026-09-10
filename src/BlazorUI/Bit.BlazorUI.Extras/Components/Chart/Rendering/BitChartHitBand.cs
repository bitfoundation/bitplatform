namespace Bit.BlazorUI;

/// <summary>
/// An invisible rectangle covering one index (category / x position) across the whole plot area.
/// Hovering it activates that index even when the pointer is nowhere near a bar or a point, which is
/// what keeps thin lines and marker-less datasets interactive. Bands are painted underneath the data
/// elements so an element directly under the pointer always wins.
/// </summary>
public sealed class BitChartHitBand
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    /// <summary>The data index this band represents.</summary>
    public int DataIndex { get; init; }
}
