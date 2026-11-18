namespace Bit.Blow;

/// <summary>
/// Where an overlay (panel, controls, minimap, attribution) is anchored inside the flow.
/// Mirrors React Flow's <c>PanelPosition</c>.
/// </summary>
public enum BlowPanelPosition
{
    /// <summary>The top-left corner.</summary>
    TopLeft,

    /// <summary>Centred along the top edge.</summary>
    TopCenter,

    /// <summary>The top-right corner.</summary>
    TopRight,

    /// <summary>The bottom-left corner.</summary>
    BottomLeft,

    /// <summary>Centred along the bottom edge.</summary>
    BottomCenter,

    /// <summary>The bottom-right corner.</summary>
    BottomRight,
}
