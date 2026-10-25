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

/// <summary>Maps <see cref="BlowPanelPosition"/> values to the CSS class suffix used by the stylesheet.</summary>
public static class BlowPanelPositionClass
{
    /// <summary>The CSS class suffix the stylesheet uses for <paramref name="position"/>.</summary>
    public static string For(BlowPanelPosition position) => position switch
    {
        BlowPanelPosition.TopCenter => "top-center",
        BlowPanelPosition.TopRight => "top-right",
        BlowPanelPosition.BottomLeft => "bottom-left",
        BlowPanelPosition.BottomCenter => "bottom-center",
        BlowPanelPosition.BottomRight => "bottom-right",
        _ => "top-left",
    };

    /// <summary>
    /// Parses the loose string form used by <c>BlowPanel.Position</c> ("top-left", "Top Right", ...).
    /// Unknown values fall back to <see cref="BlowPanelPosition.TopLeft"/>.
    /// </summary>
    public static BlowPanelPosition Parse(string? value) =>
        (value ?? string.Empty).Replace(' ', '-').Replace('_', '-').ToLowerInvariant() switch
        {
            "top-center" => BlowPanelPosition.TopCenter,
            "top-right" => BlowPanelPosition.TopRight,
            "bottom-left" => BlowPanelPosition.BottomLeft,
            "bottom-center" => BlowPanelPosition.BottomCenter,
            "bottom-right" => BlowPanelPosition.BottomRight,
            _ => BlowPanelPosition.TopLeft,
        };
}
