namespace Bit.BbScanner;

/// <summary>
/// The Aztec Code entry point: finds a bullseye, samples the symbol around it and decodes it.
/// </summary>
/// <remarks>
/// Compact and full range symbols are read at any rotation, both as printed and mirrored. An Aztec Rune (the
/// bullseye and mode message alone, carrying a single byte) is read too.
/// </remarks>
public sealed class BbAztecReader : IBbMatrixDecoder
{
    private readonly BbAztecDetector _detector = new();

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.Aztec;

    /// <inheritdoc />
    public BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);

        var result = _detector.Read(image, options.TryHarder);
        if (result is not null || !options.AllowInverted)
        {
            return result;
        }

        // Aztec is often shown light on dark on a phone's screen; the detector only knows dark on light.
        image.Invert();
        try
        {
            return _detector.Read(image, options.TryHarder);
        }
        finally
        {
            image.Invert();
        }
    }
}
