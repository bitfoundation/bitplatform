namespace Bit.Bink;

/// <summary>An image format the browser encodes for <see cref="BinkCanvas.ExportImageAsync"/>.</summary>
public enum BinkImageFormat : byte
{
    /// <summary>Lossless, with transparency. Also available without a browser, through <see cref="BinkPngExporter"/>.</summary>
    Png = 0,
    /// <summary>Lossy and small, without transparency.</summary>
    Jpeg = 1,
    /// <summary>Lossy or lossless with transparency. Not encoded by Safari.</summary>
    Webp = 2,
}
