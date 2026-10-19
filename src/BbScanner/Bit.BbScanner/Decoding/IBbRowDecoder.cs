namespace Bit.BbScanner;

/// <summary>
/// Decodes linear symbologies from a single binarised scan line.
/// </summary>
/// <remarks>
/// Row decoders are called many times per frame, once per candidate scan line, so
/// implementations must not allocate. Scratch state belongs on the instance; the pipeline
/// guarantees that one instance is only ever used by one thread at a time.
/// </remarks>
public interface IBbRowDecoder
{
    /// <summary>The symbologies this decoder can produce.</summary>
    BbBarcodeFormat Formats { get; }

    /// <summary>Attempts to decode one scan line.</summary>
    /// <param name="rowNumber">Index of the row inside the image, used to place the result.</param>
    /// <param name="row">The binarised scan line.</param>
    /// <param name="enabledFormats">The formats the caller is interested in; a subset of <see cref="Formats"/>.</param>
    /// <returns>The decoded symbol, or <see langword="null"/> when the row carries none.</returns>
    BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats);
}
