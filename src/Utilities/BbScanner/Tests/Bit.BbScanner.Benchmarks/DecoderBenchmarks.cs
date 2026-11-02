using BenchmarkDotNet.Attributes;
using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Benchmarks;

/// <summary>
/// End to end pipeline cost per frame, which is the number that decides whether a scanner keeps
/// up with the camera.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class DecoderBenchmarks
{
    private readonly Dictionary<string, FrameFactory.Frame> _frames = [];
    private BbBarcodeDecoder _allFormats = null!;
    private BbBarcodeDecoder _qrOnly = null!;
    private BbBarcodeDecoder _linearOnly = null!;
    private BbBarcodeDecoder _pdf417Only = null!;
    private BbBarcodeDecoder _allButPdf417 = null!;
    private BbBarcodeDecoder _aztecOnly = null!;
    private BbBarcodeDecoder _allButAztec = null!;
    private BbBarcodeDecoder _linearButDataBar = null!;

    /// <summary>The frame case to decode.</summary>
    [Params("QrSmall", "QrLarge", "QrNoisy", "DataMatrix", "Pdf417", "Aztec", "Code128", "Ean13", "DataBar", "DataBarExpanded", "Empty")]
    public string Case { get; set; } = "QrSmall";

    [GlobalSetup]
    public void Setup()
    {
        _frames["QrSmall"] = FrameFactory.QrCode("https://example.com/p/12345", scale: 3);
        _frames["QrLarge"] = FrameFactory.QrCode(new string('A', 300), scale: 3);
        _frames["QrNoisy"] = FrameFactory.QrCode("https://example.com/p/12345", scale: 4, noise: 35);
        _frames["DataMatrix"] = FrameFactory.DataMatrix("BIT-PLATFORM-BBSCANNER-DM-26");
        _frames["Pdf417"] = FrameFactory.Pdf417("BIT PLATFORM BBSCANNER PDF417 BENCHMARK");
        _frames["Aztec"] = FrameFactory.Aztec("M1BENCHMARK/BBSCANNER EABC123 AMSJFKKL 0641 300Y012A0042 100");
        _frames["Code128"] = FrameFactory.Linear(
            "Code 128", BbBarcodeFormat.Code128, LinearEncoders.Code128("BARCODE-SCANNER-128"), "BARCODE-SCANNER-128");
        _frames["Ean13"] = FrameFactory.Linear(
            "EAN-13", BbBarcodeFormat.Ean13, LinearEncoders.Ean13("4006381333931"), "4006381333931");
        _frames["DataBar"] = FrameFactory.Linear(
            "GS1 DataBar", BbBarcodeFormat.DataBar, LinearEncoders.DataBar("09521234543213"), "0109521234543213");
        _frames["DataBarExpanded"] = FrameFactory.Linear(
            "GS1 DataBar Expanded", BbBarcodeFormat.DataBarExpanded, LinearEncoders.DataBarExpanded("010952123454321310ABC123"), "010952123454321310ABC123");
        _frames["Empty"] = FrameFactory.Empty();

        _allFormats = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.All,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        _qrOnly = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.QrCode,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        _linearOnly = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.AllLinear,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        _pdf417Only = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.Pdf417,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        _allButPdf417 = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.All & ~BbBarcodeFormat.Pdf417,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        _aztecOnly = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.Aztec,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        _allButAztec = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.All & ~BbBarcodeFormat.Aztec,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        _linearButDataBar = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.AllLinear & ~(BbBarcodeFormat.DataBar | BbBarcodeFormat.DataBarLimited | BbBarcodeFormat.DataBarExpanded),
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _allFormats.Dispose();
        _qrOnly.Dispose();
        _linearOnly.Dispose();
        _pdf417Only.Dispose();
        _allButPdf417.Dispose();
        _aztecOnly.Dispose();
        _allButAztec.Dispose();
        _linearButDataBar.Dispose();
    }

    /// <summary>Every symbology enabled, which is the worst case and the default configuration.</summary>
    [Benchmark(Baseline = true)]
    public BbBarcodeResult? AllFormats()
    {
        var frame = _frames[Case];
        return _allFormats.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }

    /// <summary>Only QR, the most common single-symbology configuration.</summary>
    [Benchmark]
    public BbBarcodeResult? QrOnly()
    {
        var frame = _frames[Case];
        return _qrOnly.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }

    /// <summary>Only the linear symbologies, which skips binarising the whole frame.</summary>
    [Benchmark]
    public BbBarcodeResult? LinearOnly()
    {
        var frame = _frames[Case];
        return _linearOnly.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }

    /// <summary>Only PDF417: binarising the frame and locating start patterns at any orientation.</summary>
    [Benchmark]
    public BbBarcodeResult? Pdf417Only()
    {
        var frame = _frames[Case];
        return _pdf417Only.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }

    /// <summary>
    /// Every symbology but PDF417, which is what the default cost before PDF417 was read; the
    /// difference from <see cref="AllFormats"/> is what PDF417 adds to a frame.
    /// </summary>
    [Benchmark]
    public BbBarcodeResult? AllButPdf417()
    {
        var frame = _frames[Case];
        return _allButPdf417.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }

    /// <summary>Only Aztec: binarising the frame and looking for a bullseye.</summary>
    [Benchmark]
    public BbBarcodeResult? AztecOnly()
    {
        var frame = _frames[Case];
        return _aztecOnly.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }

    /// <summary>
    /// Every symbology but Aztec, which is what the default cost before Aztec was read; the difference from
    /// <see cref="AllFormats"/> is what Aztec adds to a frame.
    /// </summary>
    [Benchmark]
    public BbBarcodeResult? AllButAztec()
    {
        var frame = _frames[Case];
        return _allButAztec.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }

    /// <summary>
    /// Every linear symbology but the GS1 DataBar family - DataBar, Limited and Expanded - which is what the linear
    /// formats cost before DataBar was read; the difference from <see cref="LinearOnly"/> is what the family adds to a
    /// frame.
    /// </summary>
    [Benchmark]
    public BbBarcodeResult? LinearButDataBar()
    {
        var frame = _frames[Case];
        return _linearButDataBar.Decode(frame.Pixels, frame.Width, frame.Height, BbPixelFormat.Gray8, suppressDuplicates: false);
    }
}
