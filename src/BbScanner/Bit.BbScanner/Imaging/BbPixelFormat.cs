namespace Bit.BbScanner;

/// <summary>Layout of the pixel data handed to <see cref="BbPixelConverter"/>.</summary>
public enum BbPixelFormat
{
    /// <summary>Four bytes per pixel: red, green, blue, alpha. The layout produced by <c>CanvasRenderingContext2D.getImageData</c>.</summary>
    Rgba32 = 0,

    /// <summary>Four bytes per pixel: blue, green, red, alpha.</summary>
    Bgra32 = 1,

    /// <summary>Three bytes per pixel: red, green, blue.</summary>
    Rgb24 = 2,

    /// <summary>One byte per pixel, already grayscale.</summary>
    Gray8 = 3,
}
