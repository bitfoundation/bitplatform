namespace Bit.BlazorUI;

/// <summary>
/// Which channels the text fields of the <see cref="BitColorPicker"/> are written in.
/// </summary>
/// <remarks>
/// The mode only decides how the color is typed and read off. It says nothing about the value the picker
/// publishes, which is what <see cref="BitColorFormat"/> is for: a picker can be edited in HSL and still
/// answer in hexadecimal.
/// </remarks>
public enum BitColorInputsMode
{
    /// <summary>
    /// The hexadecimal field alongside the three Red-Green-Blue channels, which is the pair most color
    /// pickers show together.
    /// </summary>
    HexRgb,

    /// <summary>
    /// The hexadecimal field on its own.
    /// </summary>
    Hex,

    /// <summary>
    /// The Red, Green and Blue channels, each from 0 to 255.
    /// </summary>
    Rgb,

    /// <summary>
    /// Hue in degrees, saturation and lightness as percentages.
    /// </summary>
    Hsl,

    /// <summary>
    /// Hue in degrees, saturation and brightness as percentages - the model the picker itself is driven in,
    /// so the numbers here are the ones the saturation area and the hue slider move.
    /// </summary>
    Hsv
}
