namespace Bit.Bmage;

/// <summary>How <see cref="BmageAdaptiveThresholdFilter"/> sets the level each pixel is compared with.</summary>
public enum BmageAdaptiveThresholdMethod
{
    /// <summary>
    /// Sauvola's method: the local mean, lowered where the neighbourhood has little contrast. Keeps text crisp on a page
    /// lit unevenly and leaves the plain paper white instead of speckled, which is what document scans need.
    /// </summary>
    Sauvola = 0,

    /// <summary>
    /// The local mean less a fixed offset. Faster and simpler; plain areas pick up noise more easily than with Sauvola's
    /// method, but fine lines in strong contrast survive better.
    /// </summary>
    Mean,
}
