using BenchmarkDotNet.Attributes;
using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Benchmarks;

/// <summary>
/// Per-stage cost, so that a regression can be attributed to the stage that caused it rather
/// than to "the decoder got slower".
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "RatioSD")]
public class StageBenchmarks
{
    private FrameFactory.Frame _qr = null!;
    private byte[] _rgba = null!;
    private byte[] _grayscaleTarget = null!;
    private BbLuminanceBuffer _gray = null!;
    private BbBitMatrix _binary = null!;
    private BbBitRow _row = null!;

    private readonly BbHybridBinarizer _hybrid = new();
    private readonly BbGlobalHistogramBinarizer _global = new();
    private readonly BbQrCodeReader _qrReader = new();
    private readonly BbDataMatrixReader _dataMatrixReader = new();
    private readonly BbCode128Reader _code128 = new();
    private readonly BbEanUpcReader _ean = new();

    [GlobalSetup]
    public void Setup()
    {
        _qr = FrameFactory.QrCode("https://example.com/p/12345", scale: 4);
        _rgba = FrameFactory.ToRgba(_qr.Pixels);
        _grayscaleTarget = new byte[_qr.Width * _qr.Height];

        _gray = BbLuminanceBuffer.Rent(_qr.Width, _qr.Height);
        _qr.Pixels.CopyTo(_gray.Pixels);

        _binary = _hybrid.GetBlackMatrix(_gray.View)!;
        _row = new BbBitRow(_qr.Width);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _binary.Dispose();
        _gray.Dispose();
    }

    /// <summary>RGBA to luminance: the only stage that touches every byte of the source frame.</summary>
    [Benchmark]
    public void GrayscaleFromRgba() =>
        BbPixelConverter.ToGrayscale(_rgba, _grayscaleTarget, _qr.Width, _qr.Height, BbPixelFormat.Rgba32);

    /// <summary>The same conversion with two-times subsampling, which is how the pipeline downsamples.</summary>
    [Benchmark]
    public void GrayscaleFromRgbaDownsampled() =>
        BbPixelConverter.ToGrayscaleDownsampled(_rgba, _grayscaleTarget, _qr.Width, _qr.Height, BbPixelFormat.Rgba32, 2);

    /// <summary>Adaptive binarisation, used for matrix symbologies.</summary>
    [Benchmark]
    public void HybridBinarize()
    {
        using var matrix = _hybrid.GetBlackMatrix(_gray.View);
    }

    /// <summary>Single row binarisation, used for linear symbologies.</summary>
    [Benchmark]
    public bool BinarizeOneRow() => _global.TryGetBlackRow(_gray.View, _qr.Height / 2, _row);

    /// <summary>QR detection and decoding from an already binarised frame.</summary>
    [Benchmark]
    public BbSymbolDecodeResult? QrDetectAndDecode() => _qrReader.Decode(_binary, BbMatrixDecodeOptions.Live);

    /// <summary>Data Matrix detection on a frame that contains a QR code, the realistic miss case.</summary>
    [Benchmark]
    public BbSymbolDecodeResult? DataMatrixMiss() => _dataMatrixReader.Decode(_binary, BbMatrixDecodeOptions.Live);

    /// <summary>One Code 128 attempt on a row that does not contain one.</summary>
    [Benchmark]
    public BbSymbolDecodeResult? Code128Miss()
    {
        _global.TryGetBlackRow(_gray.View, _qr.Height / 2, _row);
        return _code128.DecodeRow(0, _row, BbBarcodeFormat.Code128);
    }

    /// <summary>One EAN attempt on a row that does not contain one.</summary>
    [Benchmark]
    public BbSymbolDecodeResult? EanMiss()
    {
        _global.TryGetBlackRow(_gray.View, _qr.Height / 2, _row);
        return _ean.DecodeRow(0, _row, BbBarcodeFormat.Ean13);
    }
}

/// <summary>Cost of the pure algorithmic pieces, independent of image size.</summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "RatioSD")]
public class SymbolBenchmarks
{
    private BbBitMatrix _qrMatrix = null!;
    private BbBitMatrix _dataMatrixMatrix = null!;
    private readonly BbQrDecoder _qrDecoder = new();
    private readonly BbDataMatrixReader _dataMatrixReader = new();

    [GlobalSetup]
    public void Setup()
    {
        _qrMatrix = QrEncoder.Encode("https://example.com/p/12345", BbQrErrorCorrectionLevel.M, maskPattern: 2);
        _dataMatrixMatrix = DataMatrixEncoder.Encode("BIT-PLATFORM-BBSCANNER-DM-26");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _qrMatrix.Dispose();
        _dataMatrixMatrix.Dispose();
    }

    /// <summary>QR module grid to payload: parsing, de-interleaving, Reed-Solomon and bit stream.</summary>
    [Benchmark]
    public string? QrGridToPayload() => _qrDecoder.Decode(_qrMatrix)?.Text;

    /// <summary>Data Matrix module grid to payload.</summary>
    [Benchmark]
    public string? DataMatrixGridToPayload() => _dataMatrixReader.DecodeMatrix(_dataMatrixMatrix)?.Text;
}
