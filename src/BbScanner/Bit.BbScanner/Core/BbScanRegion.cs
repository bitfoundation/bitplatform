namespace Bit.BbScanner;

/// <summary>
/// A rectangular sub-region of the camera frame that should be searched for barcodes,
/// expressed in normalized coordinates so that it is independent of the resolution
/// negotiated with the camera.
/// </summary>
/// <param name="X">Left edge, 0 (frame left) to 1 (frame right).</param>
/// <param name="Y">Top edge, 0 (frame top) to 1 (frame bottom).</param>
/// <param name="Width">Width as a fraction of the frame width.</param>
/// <param name="Height">Height as a fraction of the frame height.</param>
public readonly record struct BbScanRegion(double X, double Y, double Width, double Height)
{
    /// <summary>The whole frame.</summary>
    public static BbScanRegion Full => new(0, 0, 1, 1);

    /// <summary>A centered square covering <paramref name="fraction"/> of the shorter frame edge.</summary>
    /// <remarks>
    /// Normalized coordinates cannot describe a square on their own, because the same fraction of a
    /// wide frame's width and height are different numbers of pixels. The region therefore carries
    /// <see cref="KeepSquare"/>, and is made square for each frame it is applied to by
    /// <see cref="ForFrame"/>: on a 1280 by 720 frame, <c>CenteredSquare(0.7)</c> searches a 504
    /// by 504 pixel square.
    /// </remarks>
    /// <param name="fraction">Edge length as a fraction of the shorter frame edge, 0 exclusive to 1 inclusive.</param>
    public static BbScanRegion CenteredSquare(double fraction)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fraction, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fraction, 1);
        var offset = (1 - fraction) / 2;
        return new BbScanRegion(offset, offset, fraction, fraction) { KeepSquare = true };
    }

    /// <summary>
    /// Keep the region square in pixels whatever the frame's aspect ratio. When set, the rectangle
    /// <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/> and <see cref="Height"/> describe is
    /// narrowed along the frame's longer axis, about its centre, to the largest square it holds.
    /// </summary>
    public bool KeepSquare { get; init; }

    /// <summary><see langword="true"/> when the region covers the entire frame.</summary>
    /// <remarks>
    /// A <see cref="KeepSquare"/> region is never reported as full, since whether it is depends on
    /// the frame; resolve it with <see cref="ForFrame"/> to ask about a particular one.
    /// </remarks>
    public bool IsFull => !KeepSquare && X <= 0 && Y <= 0 && Width >= 1 && Height >= 1;

    /// <summary>Clamps the region so that it lies inside the unit square.</summary>
    public BbScanRegion Normalized()
    {
        var x = Math.Clamp(X, 0, 1);
        var y = Math.Clamp(Y, 0, 1);
        var w = Math.Clamp(Width, 0, 1 - x);
        var h = Math.Clamp(Height, 0, 1 - y);
        return this with { X = x, Y = y, Width = w, Height = h };
    }

    /// <summary>
    /// The region as it applies to a frame of the given size: normalized and, when
    /// <see cref="KeepSquare"/> is set, made square in that frame's pixels.
    /// </summary>
    /// <param name="frameWidth">Frame width in pixels.</param>
    /// <param name="frameHeight">Frame height in pixels.</param>
    public BbScanRegion ForFrame(int frameWidth, int frameHeight)
    {
        var region = Normalized();
        if (!KeepSquare || frameWidth <= 0 || frameHeight <= 0)
        {
            return region;
        }

        var side = Math.Min(region.Width * frameWidth, region.Height * frameHeight);
        var width = side / frameWidth;
        var height = side / frameHeight;
        return new BbScanRegion(
            region.X + ((region.Width - width) / 2),
            region.Y + ((region.Height - height) / 2),
            width,
            height);
    }

    /// <summary>
    /// The region in whole pixels of a frame of the given size, always at least one pixel and
    /// always inside the frame: the pipeline treats "nothing to scan" as a frame with no symbol,
    /// never as an exception.
    /// </summary>
    internal (int X, int Y, int Width, int Height) ToPixels(int frameWidth, int frameHeight)
    {
        var region = ForFrame(frameWidth, frameHeight);
        if (region.IsFull)
        {
            return (0, 0, frameWidth, frameHeight);
        }

        // Rounded rather than truncated: a fraction such as 504 / 1280 is not exact in binary, and
        // truncating its product with the frame size would lose a pixel to the representation error.
        var x = Math.Clamp((int)Math.Round(region.X * frameWidth), 0, Math.Max(0, frameWidth - 1));
        var y = Math.Clamp((int)Math.Round(region.Y * frameHeight), 0, Math.Max(0, frameHeight - 1));
        var w = Math.Clamp((int)Math.Round(region.Width * frameWidth), 1, Math.Max(1, frameWidth - x));
        var h = Math.Clamp((int)Math.Round(region.Height * frameHeight), 1, Math.Max(1, frameHeight - y));
        return (x, y, w, h);
    }
}
