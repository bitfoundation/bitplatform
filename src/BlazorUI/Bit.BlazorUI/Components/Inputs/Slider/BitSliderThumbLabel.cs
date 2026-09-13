namespace Bit.BlazorUI;

/// <summary>
/// Decides when the <see cref="BitSlider"/> shows the floating label that rides along with its thumb.
/// </summary>
public enum BitSliderThumbLabel
{
    /// <summary>
    /// Never show the floating label. This is the default.
    /// </summary>
    Off,

    /// <summary>
    /// Show the floating label only while the slider is being hovered, dragged or focused,
    /// so it stays out of the way until the value is actually being changed.
    /// </summary>
    Auto,

    /// <summary>
    /// Always show the floating label.
    /// </summary>
    On
}
