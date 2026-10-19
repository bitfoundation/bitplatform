namespace Bit.BbScanner;

/// <summary>
/// The QR Code entry point: detection followed by decoding.
/// </summary>
public sealed class BbQrCodeReader : IBbMatrixDecoder
{
    private readonly BbQrDecoder _decoder = new();
    private readonly Func<BbDetectorResult, bool> _accept;
    private BbQrDetector? _detector;
    private BbDecoderResult? _payload;

    /// <summary>Creates a reader.</summary>
    public BbQrCodeReader()
    {
        // Cached once: a lambda here would allocate a closure and a delegate on every frame.
        _accept = AcceptCandidate;
    }

    private bool AcceptCandidate(BbDetectorResult candidate)
    {
        _payload = _decoder.Decode(candidate.Bits);
        return _payload is not null;
    }

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.QrCode;

    /// <inheritdoc />
    public BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);

        var result = TryDecode(image, options.TryHarder);
        if (result is not null)
        {
            return result;
        }

        if (!options.AllowInverted)
        {
            return null;
        }

        // A light symbol on a dark background is common on screens and on some packaging. The
        // detector only understands dark on light, so the image is inverted and retried.
        image.Invert();
        try
        {
            return TryDecode(image, options.TryHarder);
        }
        finally
        {
            image.Invert();
        }
    }

    private BbSymbolDecodeResult? TryDecode(BbBitMatrix image, bool tryHarder)
    {
        if (_detector is null)
        {
            _detector = new BbQrDetector(image);
        }
        else
        {
            _detector.Reset(image);
        }

        _payload = null;
        using var detected = _detector.Detect(tryHarder, _accept);

        return detected is null || _payload is null
            ? null
            : new BbSymbolDecodeResult(_payload, BbBarcodeFormat.QrCode, detected.Points);
    }
}
