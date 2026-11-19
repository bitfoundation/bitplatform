namespace Bit.BbScanner;

/// <summary>A connected patch of ink, as a pixel bounding box.</summary>
/// <param name="Left">Left edge, inclusive.</param>
/// <param name="Top">Top edge, inclusive.</param>
/// <param name="Right">Right edge, inclusive.</param>
/// <param name="Bottom">Bottom edge, inclusive.</param>
/// <param name="Cells">Number of dark cells the patch contains.</param>
public readonly record struct BbScannerDarkRegion(int Left, int Top, int Right, int Bottom, int Cells)
{
    /// <summary>Width in pixels.</summary>
    public int Width => Right - Left + 1;

    /// <summary>Height in pixels.</summary>
    public int Height => Bottom - Top + 1;

    /// <summary>Horizontal centre in pixels.</summary>
    public float CenterX => (Left + Right) / 2.0f;

    /// <summary>Vertical centre in pixels.</summary>
    public float CenterY => (Top + Bottom) / 2.0f;
}
