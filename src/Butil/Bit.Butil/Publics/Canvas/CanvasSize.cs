namespace Bit.Butil;

/// <summary>A canvas's two sizes, which are not the same thing.</summary>
/// <param name="Width">The pixel buffer's width - what you draw into and what gets exported.</param>
/// <param name="Height">The pixel buffer's height.</param>
/// <param name="CssWidth">The width CSS is displaying it at, in CSS pixels.</param>
/// <param name="CssHeight">The height CSS is displaying it at.</param>
/// <param name="DevicePixelRatio">
/// How many device pixels one CSS pixel covers - 1 on an ordinary display, 2 or 3 on a dense one.
/// Set the buffer to the CSS size multiplied by this for a canvas that is sharp rather than
/// upscaled.
/// </param>
public record CanvasSize(int Width, int Height, double CssWidth, double CssHeight, double DevicePixelRatio);
