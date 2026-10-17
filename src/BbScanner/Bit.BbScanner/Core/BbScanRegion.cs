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
    /// <param name="fraction">Edge length as a fraction of the shorter frame edge, 0 exclusive to 1 inclusive.</param>
    public static BbScanRegion CenteredSquare(double fraction)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fraction, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fraction, 1);
        var offset = (1 - fraction) / 2;
        return new BbScanRegion(offset, offset, fraction, fraction);
    }

    /// <summary><see langword="true"/> when the region covers the entire frame.</summary>
    public bool IsFull => X <= 0 && Y <= 0 && Width >= 1 && Height >= 1;

    /// <summary>Clamps the region so that it lies inside the unit square.</summary>
    public BbScanRegion Normalized()
    {
        var x = Math.Clamp(X, 0, 1);
        var y = Math.Clamp(Y, 0, 1);
        var w = Math.Clamp(Width, 0, 1 - x);
        var h = Math.Clamp(Height, 0, 1 - y);
        return new BbScanRegion(x, y, w, h);
    }
}
