namespace Bit.BlazorUI;

/// <summary>Generic position used by axes, legend and title.</summary>
public enum BitChartPosition
{
    Top,
    Left,
    Bottom,
    Right,

    /// <summary>
    /// Cartesian axes only: draw the axis where the other one reads zero instead of along an edge, so it
    /// costs no layout space. Legends and titles have no center placement and fall back to the top.
    /// </summary>
    Center,

    /// <summary>Reserved. Nothing places itself here yet; it falls back to the default side.</summary>
    Chart
}
