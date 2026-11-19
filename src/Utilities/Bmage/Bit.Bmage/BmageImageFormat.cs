namespace Bit.Bmage;

/// <summary>Encoded image formats known to Bmage.</summary>
public enum BmageImageFormat
{
    Unknown = 0,
    Jpeg,
    Png,
    WebP,
    Avif,
    Gif,
    Bmp,
    /// <summary>Scalable Vector Graphics. Only supported for rasterisation by browser decoders and treated as untrusted input.</summary>
    Svg,
    Tiff,
    /// <summary>Windows icon: one or more images of up to 256 pixels, each stored as PNG or as a bitmap. Used for favicons.</summary>
    Ico,
}
