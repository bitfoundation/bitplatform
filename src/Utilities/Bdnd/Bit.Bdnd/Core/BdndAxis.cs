namespace Bit.Bdnd;

/// <summary>An axis a drag preview can be kept to.</summary>
public enum BdndAxis
{
    /// <summary>No axis: the preview follows the pointer freely.</summary>
    None,

    /// <summary>Sideways only: the preview keeps the height it started at.</summary>
    Horizontal,

    /// <summary>Up and down only: the preview keeps the horizontal position it started at.</summary>
    Vertical,
}
