using Bit.BbScanner;

namespace Bit.Bmage;

/// <summary>What <see cref="BmageBarcodes.Read"/> found, and which preparation found it.</summary>
/// <param name="Barcodes">The codes; empty when none was found.</param>
/// <param name="Preprocessing">The preparation that found them, or null when none did.</param>
/// <param name="ScaleX">
/// How much wider than the image given the image the codes were found in was: above 1 after
/// <see cref="BmageBarcodePreprocessing.Enlarge"/>, else 1. <see cref="CornersOf"/> maps a code's corners back.
/// </param>
/// <param name="ScaleY">
/// How much taller than the image given the image the codes were found in was. Rounding each side to whole pixels can
/// leave it a little different from <paramref name="ScaleX"/>.
/// </param>
public sealed record BmageBarcodeReadResult(IReadOnlyList<BbScannerBarcodeResult> Barcodes, BmageBarcodePreprocessing? Preprocessing, double ScaleX = 1, double ScaleY = 1)
{
    /// <summary>True when at least one code was found.</summary>
    public bool Found => Barcodes.Count > 0;

    /// <summary>The corners of <paramref name="barcode"/> in the image as it was given.</summary>
    public IReadOnlyList<BbScannerScanPoint> CornersOf(BbScannerBarcodeResult barcode)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        return ScaleX == 1 && ScaleY == 1 ? barcode.Corners : [.. barcode.Corners.Select(p => new BbScannerScanPoint((float)(p.X / ScaleX), (float)(p.Y / ScaleY)))];
    }
}
