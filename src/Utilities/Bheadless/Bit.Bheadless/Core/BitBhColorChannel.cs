namespace Bit.Bheadless;

/// <summary>A channel of a <see cref="BitBhColor"/>, and the range a <see cref="BitBhColorSlider"/> moves it in.</summary>
public enum BitBhColorChannel
{
    /// <summary>Hue, 0–360 degrees.</summary>
    Hue,

    /// <summary>HSB / HSV saturation, 0–100%.</summary>
    Saturation,

    /// <summary>HSB / HSV brightness (value), 0–100%.</summary>
    Brightness,

    /// <summary>HSL lightness, 0–100%.</summary>
    Lightness,

    /// <summary>Red, 0–255.</summary>
    Red,

    /// <summary>Green, 0–255.</summary>
    Green,

    /// <summary>Blue, 0–255.</summary>
    Blue,

    /// <summary>Opacity, 0–100%.</summary>
    Alpha,
}
