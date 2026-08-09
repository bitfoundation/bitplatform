namespace Bit.BlazorUI;

/// <summary>
/// The CSS notation the <see cref="BitColorPicker"/> writes its color value in.
/// </summary>
/// <remarks>
/// Every notation has an alpha-carrying twin. The plain ones drop the alpha channel from the value
/// string - the alpha is still tracked, and still reported through the Alpha parameter - while the
/// twins fold it into the string, which is what a single bound value needs to survive a round trip
/// through a semi-transparent color.
/// </remarks>
public enum BitColorFormat
{
    /// <summary>
    /// Six-digit hexadecimal notation: <c>#RRGGBB</c>.
    /// </summary>
    Hex,

    /// <summary>
    /// Eight-digit hexadecimal notation, whose last pair is the alpha channel: <c>#RRGGBBAA</c>.
    /// </summary>
    HexAlpha,

    /// <summary>
    /// Functional RGB notation: <c>rgb(255,0,0)</c>.
    /// </summary>
    Rgb,

    /// <summary>
    /// Functional RGB notation with an alpha channel: <c>rgba(255,0,0,0.5)</c>.
    /// </summary>
    Rgba,

    /// <summary>
    /// Functional HSL notation: <c>hsl(0,100%,50%)</c>.
    /// </summary>
    Hsl,

    /// <summary>
    /// Functional HSL notation with an alpha channel: <c>hsla(0,100%,50%,0.5)</c>.
    /// </summary>
    Hsla,

    /// <summary>
    /// Functional HSV notation: <c>hsv(0,100%,100%)</c>. It is the model the picker itself is built
    /// on, but unlike the others it is not a notation any browser understands.
    /// </summary>
    Hsv,

    /// <summary>
    /// Functional HSV notation with an alpha channel: <c>hsva(0,100%,100%,0.5)</c>.
    /// </summary>
    Hsva
}
