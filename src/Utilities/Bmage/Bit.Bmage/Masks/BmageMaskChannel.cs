namespace Bit.Bmage;

/// <summary>Which channel of an image a <see cref="BmageBitmapMask"/> reads.</summary>
public enum BmageMaskChannel
{
    /// <summary>The alpha channel: opaque is covered. A cut-out from a background removal model.</summary>
    Alpha = 0,
    /// <summary>The luminance, weighted by alpha: white is covered. A greyscale matte or a painted mask.</summary>
    Luminance,
}
