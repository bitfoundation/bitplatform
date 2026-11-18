namespace Bit.Bmage;

/// <summary>How a redaction hides the underlying content.</summary>
public enum BmageRedactionMode
{
    /// <summary>Paints a solid block. Irreversible and unambiguous; the safest choice.</summary>
    Solid = 0,
    /// <summary>Averages blocks of pixels. Looks softer but is still irreversible at a sensible block size.</summary>
    Pixelate,
    /// <summary>Blurs the region. Use only for de-emphasis, never for secrets: strong blurs can sometimes be inverted.</summary>
    Blur,
}
