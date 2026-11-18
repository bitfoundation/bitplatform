namespace Bit.Blow;

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
