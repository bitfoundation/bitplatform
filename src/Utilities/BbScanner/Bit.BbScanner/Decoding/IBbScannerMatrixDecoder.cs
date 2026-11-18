namespace Bit.BbScanner;

/// <summary>
/// Decodes two dimensional symbologies from a binarised image.
/// </summary>
/// <remarks>
/// Matrix decoders own both detection (finding the symbol in the image) and decoding (reading
/// the module grid). Splitting them at this boundary rather than a finer one keeps each
/// symbology free to use the detection strategy that suits it, which is what the specifications
/// actually require: a QR finder pattern search and a Data Matrix L-shape trace have almost
/// nothing in common.
/// </remarks>
public interface IBbScannerMatrixDecoder
{
    /// <summary>The symbologies this decoder can produce.</summary>
    BbScannerBarcodeFormat Formats { get; }

    /// <summary>Attempts to find and decode a symbol.</summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="options">Tuning that trades accuracy against latency.</param>
    /// <returns>The decoded symbol, or <see langword="null"/> when the image carries none.</returns>
    BbScannerSymbolDecodeResult? Decode(BbScannerBitMatrix image, BbScannerMatrixDecodeOptions options);
}
