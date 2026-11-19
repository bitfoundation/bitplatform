namespace Bit.Bmage;

/// <summary>A way <see cref="BmageBarcodes.Read"/> prepares an image before decoding it again.</summary>
public enum BmageBarcodePreprocessing
{
    /// <summary>The image as it is.</summary>
    None = 0,

    /// <summary>Sharpened with an unsharp mask: a code photographed slightly out of focus.</summary>
    Sharpen,

    /// <summary>
    /// Enlarged to <see cref="BmageBarcodeReadOptions.MinimumSize"/> pixels across with bicubic interpolation: a soft code
    /// whose modules are only a few pixels wide, from a thumbnail or a distant shot, gets room for its edges. Skipped for
    /// an image that is already that large.
    /// </summary>
    Enlarge,
}
