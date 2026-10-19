namespace Bit.BbScanner;

/// <summary>
/// The output of a matrix symbology detector: a sampled module grid plus the corner points the
/// grid was read from, in image coordinates.
/// </summary>
/// <param name="Bits">The sampled module grid. The receiver owns and must dispose it.</param>
/// <param name="Points">Corner points in image coordinates.</param>
public sealed record BbDetectorResult(BbBitMatrix Bits, BbScanPoint[] Points) : IDisposable
{
    /// <inheritdoc />
    public void Dispose() => Bits.Dispose();
}
