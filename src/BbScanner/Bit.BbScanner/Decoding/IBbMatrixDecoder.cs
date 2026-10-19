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
public interface IBbMatrixDecoder
{
    /// <summary>The symbologies this decoder can produce.</summary>
    BbBarcodeFormat Formats { get; }

    /// <summary>Attempts to find and decode a symbol.</summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="options">Tuning that trades accuracy against latency.</param>
    /// <returns>The decoded symbol, or <see langword="null"/> when the image carries none.</returns>
    BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options);
}

/// <summary>Per-attempt tuning handed to a <see cref="IBbMatrixDecoder"/>.</summary>
/// <param name="TryHarder">
/// When <see langword="true"/> the decoder may spend significantly more time, for example by
/// trying rotated variants or relaxing detector tolerances. The pipeline sets this for still
/// images and clears it for live camera frames.
/// </param>
/// <param name="AllowInverted">When <see langword="true"/> the decoder also tries a light-on-dark symbol.</param>
public readonly record struct BbMatrixDecodeOptions(bool TryHarder, bool AllowInverted)
{
    /// <summary>The defaults used for live camera frames: fast, no inverted retry.</summary>
    public static BbMatrixDecodeOptions Live => new(TryHarder: false, AllowInverted: false);

    /// <summary>The defaults used for still images: exhaustive.</summary>
    public static BbMatrixDecodeOptions Thorough => new(TryHarder: true, AllowInverted: true);
}
