namespace Bit.BbScanner;

/// <summary>
/// The QR Code entry point: detection followed by decoding. It reads Micro QR too, round the finder patterns the QR
/// Code search finds, so that one search serves both.
/// </summary>
public sealed class BbScannerQrCodeReader : IBbScannerMatrixDecoder, IBbScannerLuminanceMatrixDecoder
{
    private readonly BbScannerQrDecoder _decoder = new();
    private readonly BbScannerMicroQrDetector _microDetector = new();
    private readonly Func<BbScannerDetectorResult, bool> _accept;
    private readonly Func<BbScannerDetectorResult, bool> _acceptMicro;
    private readonly BbScannerBarcodeFormat _formats;
    private BbScannerQrDetector? _detector;
    private BbScannerDecoderResult? _payload;

    /// <summary>Creates a reader for QR Code.</summary>
    public BbScannerQrCodeReader()
        : this(BbScannerBarcodeFormat.QrCode)
    {
    }

    /// <summary>Creates a reader for QR Code, Micro QR, or both.</summary>
    /// <param name="formats"><see cref="BbScannerBarcodeFormat.QrCode"/>, <see cref="BbScannerBarcodeFormat.MicroQrCode"/> or both.</param>
    public BbScannerQrCodeReader(BbScannerBarcodeFormat formats)
    {
        _formats = formats & (BbScannerBarcodeFormat.QrCode | BbScannerBarcodeFormat.MicroQrCode);
        if (_formats == BbScannerBarcodeFormat.None)
        {
            throw new ArgumentException("The reader reads QR Code and Micro QR.", nameof(formats));
        }

        // Cached once: a lambda here would allocate a closure and a delegate on every frame.
        _accept = AcceptCandidate;
        _acceptMicro = AcceptMicroCandidate;
    }

    private bool AcceptCandidate(BbScannerDetectorResult candidate)
    {
        _payload = _decoder.Decode(candidate.Bits);
        return _payload is not null;
    }

    private bool AcceptMicroCandidate(BbScannerDetectorResult candidate)
    {
        _payload = BbScannerMicroQrDecoder.Decode(candidate.Bits);
        return _payload is not null;
    }

    /// <inheritdoc />
    public BbScannerBarcodeFormat Formats => _formats;

    /// <inheritdoc />
    BbScannerSymbolDecodeResult? IBbScannerLuminanceMatrixDecoder.Decode(BbScannerBitMatrix image, in BbScannerLuminanceView luminance, BbScannerMatrixDecodeOptions options)
        => Decode(image, luminance, options);

    /// <inheritdoc />
    public BbScannerSymbolDecodeResult? Decode(BbScannerBitMatrix image, BbScannerMatrixDecodeOptions options) => Decode(image, default, options);

    /// <summary>
    /// Decodes with the luminance plane the image was binarised from, which a grid too fine for the binarised image is
    /// sampled from when it is not empty. It is handed down, never kept: the reader holds no reference to a frame once
    /// read.
    /// </summary>
    private BbScannerSymbolDecodeResult? Decode(BbScannerBitMatrix image, in BbScannerLuminanceView luminance, BbScannerMatrixDecodeOptions options)
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

    private BbScannerSymbolDecodeResult? TryDecode(BbScannerBitMatrix image, in BbScannerLuminanceView luminance, bool tryHarder, bool inverted)
    {
        if (_detector is null)
        {
            _detector = new BbScannerQrDetector(image);
        }
        else
        {
            _detector.Reset(image);
        }

        _payload = null;
        if ((_formats & BbScannerBarcodeFormat.QrCode) != 0)
        {
            using var detected = _detector.Detect(tryHarder, _accept, luminance, inverted);
            if (detected is not null && _payload is not null)
            {
                return new BbScannerSymbolDecodeResult(_payload, BbScannerBarcodeFormat.QrCode, detected.Points);
            }
        }

        if ((_formats & BbScannerBarcodeFormat.MicroQrCode) == 0)
        {
            return null;
        }

        // No QR symbol: the finder patterns the search found are tried as Micro QR symbols' one each.
        var centers = (_formats & BbScannerBarcodeFormat.QrCode) != 0 ? _detector.FinderCenters : _detector.ScanFinders(tryHarder);
        _payload = null;
        using var micro = _microDetector.Detect(image, centers, _acceptMicro, luminance, inverted);
        return micro is null || _payload is null
            ? null
            : new BbScannerSymbolDecodeResult(_payload, BbScannerBarcodeFormat.MicroQrCode, micro.Points);
    }
}
