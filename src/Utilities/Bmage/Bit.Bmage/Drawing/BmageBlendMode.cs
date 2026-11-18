namespace Bit.Bmage;

/// <summary>How a drawing operation, or a layer, combines with what is already on the canvas.</summary>
/// <remarks>
/// <para>
/// Everything from <see cref="Multiply"/> on follows the W3C Compositing and Blending Level 1 specification, the same
/// definitions as CSS <c>mix-blend-mode</c>, the canvas <c>globalCompositeOperation</c> and the blend modes of image
/// editors: the blend function decides the colour where the two overlap, weighted by the destination's alpha, and the
/// result is composited source-over. A transparent destination therefore shows the source unchanged, whatever the mode.
/// </para>
/// <para>
/// The separable modes work on each channel alone; <see cref="Hue"/>, <see cref="Saturation"/>, <see cref="Color"/> and
/// <see cref="Luminosity"/> work on the colour as a whole.
/// </para>
/// </remarks>
public enum BmageBlendMode
{
    /// <summary>Standard alpha compositing.</summary>
    SourceOver = 0,
    /// <summary>Replaces the destination pixels, including alpha.</summary>
    Copy,
    /// <summary>Removes coverage from the destination, making it transparent (an eraser).</summary>
    Erase,
    /// <summary>Multiplies the colours: never lighter than either, white leaves the destination unchanged.</summary>
    Multiply,
    /// <summary>Inverts, multiplies and inverts again: never darker than either, black leaves the destination unchanged.</summary>
    Screen,
    /// <summary>Multiplies the destination's darks and screens its lights, raising contrast.</summary>
    Overlay,
    /// <summary>Keeps the darker of the two, channel by channel.</summary>
    Darken,
    /// <summary>Keeps the lighter of the two, channel by channel.</summary>
    Lighten,
    /// <summary>Brightens the destination to reflect the source.</summary>
    ColorDodge,
    /// <summary>Darkens the destination to reflect the source.</summary>
    ColorBurn,
    /// <summary>Multiplies or screens depending on the source, like a harsh spotlight.</summary>
    HardLight,
    /// <summary>Darkens or lightens depending on the source, like a diffuse spotlight.</summary>
    SoftLight,
    /// <summary>The absolute difference of the colours.</summary>
    Difference,
    /// <summary>Like <see cref="Difference"/> with lower contrast.</summary>
    Exclusion,
    /// <summary>The source's hue with the destination's saturation and luminosity.</summary>
    Hue,
    /// <summary>The source's saturation with the destination's hue and luminosity.</summary>
    Saturation,
    /// <summary>The source's hue and saturation with the destination's luminosity: tints a photo, keeping its shading.</summary>
    Color,
    /// <summary>The source's luminosity with the destination's hue and saturation.</summary>
    Luminosity,
    /// <summary>Adds the colours, clipping at white (linear dodge): light leaks, glows and fireworks.</summary>
    Add,
}
