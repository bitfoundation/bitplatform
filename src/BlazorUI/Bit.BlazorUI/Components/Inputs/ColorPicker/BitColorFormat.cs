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
    Hsva,

    /// <summary>
    /// Functional HWB notation: <c>hwb(0 0% 0%)</c>. CSS only defines the space-separated syntax for it,
    /// so that is the one written.
    /// </summary>
    Hwb,

    /// <summary>
    /// Functional HWB notation with an alpha channel: <c>hwb(0 0% 0% / 0.5)</c>. CSS has no
    /// <c>hwba()</c> function - the alpha is written into <c>hwb()</c> itself, after a slash.
    /// </summary>
    Hwba,

    /// <summary>
    /// Functional Oklab notation: <c>oklab(0.6279 0.2249 0.1258)</c>. Oklab is a perceptually uniform
    /// color space, so the same numeric step covers the same visual difference wherever it is taken.
    /// </summary>
    Oklab,

    /// <summary>
    /// Functional Oklab notation with an alpha channel: <c>oklab(0.6279 0.2249 0.1258 / 0.5)</c>.
    /// </summary>
    Oklaba,

    /// <summary>
    /// Functional Oklch notation: <c>oklch(0.6279 0.2577 29.23)</c>. It is the polar form of
    /// <see cref="Oklab"/>, and the notation modern design tokens are increasingly written in.
    /// </summary>
    Oklch,

    /// <summary>
    /// Functional Oklch notation with an alpha channel: <c>oklch(0.6279 0.2577 29.23 / 0.5)</c>.
    /// </summary>
    Oklcha
}
