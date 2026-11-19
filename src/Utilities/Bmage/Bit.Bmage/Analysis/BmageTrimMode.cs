namespace Bit.Bmage;

/// <summary>What counts as background when trimming an image.</summary>
public enum BmageTrimMode
{
    /// <summary>
    /// Transparent pixels when any corner of the image is transparent, otherwise the colour of the top left pixel: what
    /// a cut-out on transparency and a scan on a white page each expect.
    /// </summary>
    Auto = 0,
    /// <summary>Pixels whose alpha is within the tolerance of fully transparent.</summary>
    Transparent,
    /// <summary>Pixels within the tolerance of <see cref="BmageTrimOptions.Color"/>, or of the top left pixel when that is null.</summary>
    Color,
}
