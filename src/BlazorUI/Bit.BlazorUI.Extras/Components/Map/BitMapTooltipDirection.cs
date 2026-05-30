namespace Bit.BlazorUI;

/// <summary>
/// Tooltip placement direction for <see cref="BitMapMarker.TooltipDirection"/>.
/// Currently honored by the Leaflet provider; other providers ignore it.
/// </summary>
public enum BitMapTooltipDirection
{
    /// <summary>Let the provider pick the best direction.</summary>
    Auto,

    /// <summary>Above the marker.</summary>
    Top,

    /// <summary>Below the marker.</summary>
    Bottom,

    /// <summary>To the left of the marker.</summary>
    Left,

    /// <summary>To the right of the marker.</summary>
    Right,

    /// <summary>Anchored at the marker center.</summary>
    Center,
}
