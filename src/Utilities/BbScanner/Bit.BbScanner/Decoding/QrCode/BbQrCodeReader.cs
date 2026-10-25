namespace Bit.BbScanner;

/// <summary>
/// The QR Code entry point: detection followed by decoding. It reads Micro QR too, round the finder patterns the QR
/// Code search finds, so that one search serves both.
/// </summary>
public sealed class BbQrCodeReader : IBbMatrixDecoder, IBbLuminanceMatrixDecoder
{
    private readonly BbQrDecoder _decoder = new();
    private readonly BbMicroQrDetector _microDetector = new();
    private readonly Func<BbDetectorResult, bool> _accept;
    private readonly Func<BbDetectorResult, bool> _acceptMicro;
    private readonly BbBarcodeFormat _formats;
    private BbQrDetector? _detector;
    private BbDecoderResult? _payload;

    /// <summary>Creates a reader for QR Code.</summary>
    public BbQrCodeReader()
        : this(BbBarcodeFormat.QrCode)
    {
    }

    /// <summary>Creates a reader for QR Code, Micro QR, or both.</summary>
    /// <param name="formats"><see cref="BbBarcodeFormat.QrCode"/>, <see cref="BbBarcodeFormat.MicroQrCode"/> or both.</param>
    public BbQrCodeReader(BbBarcodeFormat formats)
    {
        _formats = formats & (BbBarcodeFormat.QrCode | BbBarcodeFormat.MicroQrCode);
        if (_formats == BbBarcodeFormat.None)
        {
            throw new ArgumentException("The reader reads QR Code and Micro QR.", nameof(formats));
        }

        // Cached once: a lambda here would allocate a closure and a delegate on every frame.
        _accept = AcceptCandidate;
        _acceptMicro = AcceptMicroCandidate;
    }

    private bool AcceptCandidate(BbDetectorResult candidate)
    {
        _payload = _decoder.Decode(candidate.Bits);
        return _payload is not null;
    }

    private bool AcceptMicroCandidate(BbDetectorResult candidate)
    {
        _payload = BbMicroQrDecoder.Decode(candidate.Bits);
        return _payload is not null;
    }

    /// <inheritdoc />
    public BbBarcodeFormat Formats => _formats;

    /// <inheritdoc />
    BbSymbolDecodeResult? IBbLuminanceMatrixDecoder.Decode(BbBitMatrix image, in BbLuminanceView luminance, BbMatrixDecodeOptions options)
        => Decode(image, luminance, options);

    /// <inheritdoc />
    public BbSymbolDecodeResult? Decode(BbBitMatrix image, BbMatrixDecodeOptions options) => Decode(image, default, options);

    /// <summary>
    /// Decodes with the luminance plane the image was binarised from, which a grid too fine for the binarised image is
    /// sampled from when it is not empty. It is handed down, never kept: the reader holds no reference to a frame once
    /// read.
    /// </summary>
    private BbSymbolDecodeResult? Decode(BbBitMatrix image, in BbLuminanceView luminance, BbMatrixDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);

        var result = TryDecode(image, luminance, options.TryHarder, inverted: false);
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
            return TryDecode(image, luminance, options.TryHarder, inverted: true);
        }
        finally
        {
            image.Invert();
        }
    }

    private BbSymbolDecodeResult? TryDecode(BbBitMatrix image, in BbLuminanceView luminance, bool tryHarder, bool inverted)
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
        if ((_formats & BbBarcodeFormat.QrCode) != 0)
        {
            using var detected = _detector.Detect(tryHarder, _accept, luminance, inverted);
            if (detected is not null && _payload is not null)
            {
                return new BbSymbolDecodeResult(_payload, BbBarcodeFormat.QrCode, detected.Points);
            }
        }

        if ((_formats & BbBarcodeFormat.MicroQrCode) == 0)
        {
            return null;
        }

        // No QR symbol: the finder patterns the search found are tried as Micro QR symbols' one each.
        var centers = (_formats & BbBarcodeFormat.QrCode) != 0 ? _detector.FinderCenters : _detector.ScanFinders(tryHarder);
        _payload = null;
        using var micro = _microDetector.Detect(image, centers, _acceptMicro, luminance, inverted);
        return micro is null || _payload is null
            ? null
            : new BbSymbolDecodeResult(_payload, BbBarcodeFormat.MicroQrCode, micro.Points);
    }
}
