namespace Bit.BbScanner;

/// <summary>
/// A matrix decoder that can also use the luminance plane its binarised image was made from, for
/// a symbol it has to resample: interpolating luminance keeps the sub-pixel edge positions that
/// binarisation discards.
/// </summary>
internal interface IBbScannerLuminanceMatrixDecoder
{
    /// <summary>Attempts to find and decode a symbol.</summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="luminance">The luminance plane <paramref name="image"/> was binarised from, pixel for pixel.</param>
    /// <param name="options">Tuning that trades accuracy against latency.</param>
    BbScannerSymbolDecodeResult? Decode(BbScannerBitMatrix image, in BbScannerLuminanceView luminance, BbScannerMatrixDecodeOptions options);
}
