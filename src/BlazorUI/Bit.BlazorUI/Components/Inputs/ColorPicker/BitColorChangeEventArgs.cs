namespace Bit.BlazorUI;

/// <summary>
/// Describes the color a <see cref="BitColorPicker"/> has just moved to.
/// </summary>
/// <remarks>
/// The <see cref="Color"/> is the same string the Color parameter carries, in whatever format the
/// picker is writing. The remaining members are that same color in every other notation, so a handler
/// that needs a different one does not have to convert it back.
/// </remarks>
public class BitColorChangeEventArgs
{
    public void Deconstruct(out string? color, out double alpha)
    {
        color = Color;
        alpha = Alpha;
    }

    /// <summary>
    /// The main color value of the changed color in the same format as the Color parameter of the ColorPicker.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// The alpha value of the changed color, from 0 (fully transparent) to 1 (fully opaque).
    /// </summary>
    public double Alpha { get; set; }

    /// <summary>
    /// The changed color in six-digit hexadecimal notation, e.g. <c>#FF0000</c>.
    /// </summary>
    public string? Hex { get; set; }

    /// <summary>
    /// The changed color in eight-digit hexadecimal notation, whose last pair is the alpha channel,
    /// e.g. <c>#FF000080</c>.
    /// </summary>
    public string? HexAlpha { get; set; }

    /// <summary>
    /// The changed color in functional RGB notation, e.g. <c>rgb(255,0,0)</c>.
    /// </summary>
    public string? Rgb { get; set; }

    /// <summary>
    /// The changed color in functional RGB notation with its alpha channel, e.g. <c>rgba(255,0,0,0.5)</c>.
    /// </summary>
    public string? Rgba { get; set; }

    /// <summary>
    /// The changed color as hue (0-360), saturation and lightness (both 0-1).
    /// </summary>
    public (double Hue, double Saturation, double Lightness) Hsl { get; set; }

    /// <summary>
    /// The changed color as hue (0-360), saturation and value (both 0-1).
    /// </summary>
    public (double Hue, double Saturation, double Value) Hsv { get; set; }
}
