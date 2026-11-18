namespace Bit.Bmage;

/// <summary>A change to the hue, saturation and lightness of one <see cref="BmageHueBand"/>.</summary>
/// <param name="Hue">Degrees to turn the band's hue, clamped to [-60, 60].</param>
/// <param name="Saturation">Relative change of saturation in [-1,1]: -1 removes the colour, 1 doubles it.</param>
/// <param name="Lightness">Shift of lightness in [-1,1]: towards black when negative, white when positive.</param>
public readonly record struct BmageHslShift(float Hue = 0, float Saturation = 0, float Lightness = 0)
{
    /// <summary>True when the shift changes nothing.</summary>
    public bool IsZero => Hue == 0 && Saturation == 0 && Lightness == 0;

    internal BmageHslShift Clamped() => new(
        Math.Clamp(float.IsNaN(Hue) ? 0 : Hue, -60f, 60f),
        Math.Clamp(float.IsNaN(Saturation) ? 0 : Saturation, -1f, 1f),
        Math.Clamp(float.IsNaN(Lightness) ? 0 : Lightness, -1f, 1f));
}
