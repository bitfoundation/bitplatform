namespace Bit.BbScanner.Tests;

/// <summary>
/// Exercises the contracts an application depends on: option validation, disposal, the duplicate
/// filter, the reported geometry, and the promise that a frame that finds nothing allocates
/// nothing.
/// </summary>
[DoNotParallelize]
[TestClass]
public class PublicApiTests
{
    private static BbLuminanceBuffer Qr(string content = "API") =>
        SyntheticImage.FromMatrix(QrEncoder.Encode(content), scale: 5);

    [TestMethod]
    public void OptionsValidateTheirRanges()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BbScannerOptions { MaxScanLines = 0 }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BbScannerOptions { DownsampleFactor = 0 }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BbScannerOptions { DownsampleFactor = 9 }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new BbScannerOptions { DuplicateSuppressionWindow = TimeSpan.FromSeconds(-1) }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BbScannerOptions { ItfLengths = [7] }.Validate());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BbScannerOptions { ItfLengths = [0] }.Validate());

        new BbScannerOptions().Validate();
        BbScannerOptions.ForStillImages().Validate();
    }

    [TestMethod]
    public void CloneCopiesEverySetting()
    {
        var options = new BbScannerOptions
        {
            Formats = BbBarcodeFormat.Ean13,
            Region = new BbScanRegion(0.1, 0.2, 0.3, 0.4),
            MaxScanLines = 7,
            DownsampleFactor = 3,
            TryHarder = true,
            AllowInverted = true,
            TryReversedRows = false,
            TryVerticalLines = false,
            ItfLengths = [14],
            Code39CheckDigit = true,
            Code39ExtendedMode = true,
            DuplicateSuppressionWindow = TimeSpan.FromSeconds(9),
        };

        var clone = options.Clone();

        Assert.AreEqual(options.Formats, clone.Formats);
        Assert.AreEqual(options.Region, clone.Region);
        Assert.AreEqual(options.MaxScanLines, clone.MaxScanLines);
        Assert.AreEqual(options.DownsampleFactor, clone.DownsampleFactor);
        Assert.AreEqual(options.TryHarder, clone.TryHarder);
        Assert.AreEqual(options.AllowInverted, clone.AllowInverted);
        Assert.AreEqual(options.TryReversedRows, clone.TryReversedRows);
        Assert.AreEqual(options.TryVerticalLines, clone.TryVerticalLines);
        Assert.AreSequenceEqual(options.ItfLengths, clone.ItfLengths);
        Assert.AreEqual(options.Code39CheckDigit, clone.Code39CheckDigit);
        Assert.AreEqual(options.Code39ExtendedMode, clone.Code39ExtendedMode);
        Assert.AreEqual(options.DuplicateSuppressionWindow, clone.DuplicateSuppressionWindow);
    }

    [TestMethod]
    public void TheDecoderTakesACopyOfItsOptions()
    {
        var options = new BbScannerOptions { Formats = BbBarcodeFormat.QrCode };
        using var decoder = new BbBarcodeDecoder(options);

        // Editing the caller's instance afterwards must not reconfigure a running scanner.
        options.Formats = BbBarcodeFormat.None;

        using var image = Qr();
        Assert.IsNotNull(decoder.Decode(image.View, suppressDuplicates: false));
    }

    [TestMethod]
    public void DisposedDecodersRefuseToWork()
    {
        var decoder = new BbBarcodeDecoder();
        decoder.Dispose();
        decoder.Dispose();

        using var image = Qr();
        Assert.ThrowsExactly<ObjectDisposedException>(() => decoder.Decode(image.View));
    }

    [TestMethod]
    public void DecodeRejectsImpossibleDimensions()
    {
        using var decoder = new BbBarcodeDecoder();
        var pixels = new byte[64 * 64];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => decoder.Decode(pixels, 0, 64, BbPixelFormat.Gray8));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => decoder.Decode(pixels, 64, -1, BbPixelFormat.Gray8));
        Assert.ThrowsExactly<ArgumentException>(() => decoder.Decode(pixels, 4, 4, BbPixelFormat.Gray8));
    }

    [TestMethod]
    public void TheDuplicateWindowCanBeChangedWhileRunning()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            DuplicateSuppressionWindow = TimeSpan.FromMinutes(5),
        });

        using var image = Qr("WINDOW");
        Assert.IsNotNull(decoder.Decode(image.View));
        Assert.IsNull(decoder.Decode(image.View));

        decoder.DuplicateSuppressionWindow = TimeSpan.Zero;
        Assert.IsNotNull(decoder.Decode(image.View));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => decoder.DuplicateSuppressionWindow = TimeSpan.FromTicks(-1));
    }

    [TestMethod]
    public void DuplicateSuppressionIsKeyedOnValueAndSymbologyTogether()
    {
        var filter = new BbDuplicateFilter(TimeSpan.FromSeconds(2));

        Assert.IsTrue(filter.ShouldReport("A", BbBarcodeFormat.QrCode, 0));
        Assert.IsFalse(filter.ShouldReport("A", BbBarcodeFormat.QrCode, 500));

        // A different symbology carrying the same text is a different symbol.
        Assert.IsTrue(filter.ShouldReport("A", BbBarcodeFormat.Code128, 600));

        // A different value reports at once rather than waiting out the window.
        Assert.IsTrue(filter.ShouldReport("B", BbBarcodeFormat.Code128, 700));

        filter.Reset();
        Assert.IsTrue(filter.ShouldReport("B", BbBarcodeFormat.Code128, 750));
    }

    [TestMethod]
    public void HoldingASymbolInViewKeepsItSuppressed()
    {
        var filter = new BbDuplicateFilter(TimeSpan.FromSeconds(1));
        Assert.IsTrue(filter.ShouldReport("HELD", BbBarcodeFormat.QrCode, 0));

        // Reported once, then suppressed for as long as it keeps being seen, rather than firing
        // again every time the window elapses.
        for (var t = 500L; t < 10_000; t += 500)
        {
            Assert.IsFalse(filter.ShouldReport("HELD", BbBarcodeFormat.QrCode, t));
        }

        // Once it leaves the view for longer than the window, it reports again.
        Assert.IsTrue(filter.ShouldReport("HELD", BbBarcodeFormat.QrCode, 20_000));
    }

    [TestMethod]
    public void TwoSymbolsInViewAreEachReportedOnceHoweverTheFramesAlternate()
    {
        var filter = new BbDuplicateFilter(TimeSpan.FromSeconds(2));

        // A label with two codes decodes one or the other depending on the noise in each frame.
        Assert.IsTrue(filter.ShouldReport("LABEL-128", BbBarcodeFormat.Code128, 0));
        Assert.IsTrue(filter.ShouldReport("4006381333931", BbBarcodeFormat.Ean13, 66));

        for (var t = 133L; t < 5_000; t += 66)
        {
            var code128 = (t / 66) % 2 == 0;
            Assert.IsFalse(code128
                ? filter.ShouldReport("LABEL-128", BbBarcodeFormat.Code128, t)
                : filter.ShouldReport("4006381333931", BbBarcodeFormat.Ean13, t));
        }
    }

    [TestMethod]
    public void TheFilterForgetsTheSymbolSeenLongestAgoOnceItIsFull()
    {
        var filter = new BbDuplicateFilter(TimeSpan.FromMinutes(1));

        for (var i = 0; i < 64; i++)
        {
            Assert.IsTrue(filter.ShouldReport($"ITEM-{i}", BbBarcodeFormat.Code128, i));
        }

        // The most recent ones are still suppressed; the first ones were forgotten to make room.
        Assert.IsFalse(filter.ShouldReport("ITEM-63", BbBarcodeFormat.Code128, 100));
        Assert.IsTrue(filter.ShouldReport("ITEM-0", BbBarcodeFormat.Code128, 101));
    }

    [TestMethod]
    public void AZeroWindowReportsEveryFrame()
    {
        var filter = new BbDuplicateFilter(TimeSpan.Zero);
        Assert.IsTrue(filter.ShouldReport("X", BbBarcodeFormat.QrCode, 0));
        Assert.IsTrue(filter.ShouldReport("X", BbBarcodeFormat.QrCode, 0));
    }

    [TestMethod]
    public void ResultsCarryTheirGeometryInFrameCoordinates()
    {
        using var qr = Qr("GEOMETRY");
        using var frame = ImageTransforms.Frame(qr, 640, 480);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.QrCode,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        var result = decoder.Decode(frame.View, suppressDuplicates: false);

        Assert.IsNotNull(result);
        Assert.AreEqual(640, result.FrameWidth);
        Assert.AreEqual(480, result.FrameHeight);
        Assert.IsNotEmpty(result.Corners);

        // The symbol was centred, so its box must be too.
        var box = result.BoundingBox;
        Assert.IsInRange(640 / 2.0 - 40, (640 / 2.0) + 40, box.Center.X);
        Assert.IsInRange(480 / 2.0 - 40, (480 / 2.0) + 40, box.Center.Y);
        Assert.IsInRange(qr.Width * 0.4, qr.Width, box.Width);
        Assert.AreEqual(BbBarcodeFormat.QrCode, result.Format);
        Assert.AreEqual("GEOMETRY", result.Text);
        Assert.IsNotNull(result.Metadata);
        Assert.AreEqual("M", result.Metadata.ErrorCorrectionLevel);
    }

    [TestMethod]
    public void PointsFromARegionAreReportedInFullFrameCoordinates()
    {
        using var qr = Qr("REGION");
        using var frame = BbLuminanceBuffer.Rent(640, 480);
        frame.Pixels.Fill(255);
        ImageTransforms.Paste(qr, frame, 400, 300);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.QrCode,
            Region = new BbScanRegion(0.5, 0.5, 0.5, 0.5),
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        var result = decoder.Decode(frame.View, suppressDuplicates: false);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.BoundingBox.X >= 380, $"Expected the box near x=400, got {result.BoundingBox}.");
        Assert.IsTrue(result.BoundingBox.Y >= 280, $"Expected the box near y=300, got {result.BoundingBox}.");
    }

    [TestMethod]
    [DataRow(1.0, 0.0, 0.5, 0.5)]
    [DataRow(0.0, 1.0, 0.5, 0.5)]
    [DataRow(0.5, 0.5, 0.0, 0.0)]
    [DataRow(0.999, 0.999, 1.0, 1.0)]
    public void ADegenerateRegionYieldsNoSymbolRatherThanAnException(double x, double y, double w, double h)
    {
        // A region on the far edge of the frame, or one with no area, is a configuration a
        // caller can reach by arithmetic; the frame loop must survive it.
        using var qr = Qr("EDGE");
        using var frame = ImageTransforms.Frame(qr, 320, 240);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Region = new BbScanRegion(x, y, w, h),
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        Assert.IsNull(decoder.Decode(frame.View, suppressDuplicates: false));
        Assert.AreEqual(1, decoder.FramesDecoded);
    }

    [TestMethod]
    public void ScanRectFromPointsEnclosesThemAll()
    {
        ReadOnlySpan<BbScanPoint> points = [new(10, 20), new(30, 5), new(15, 40)];
        var rect = BbScanRect.FromPoints(points);

        Assert.AreEqual(10, rect.X);
        Assert.AreEqual(5, rect.Y);
        Assert.AreEqual(20, rect.Width);
        Assert.AreEqual(35, rect.Height);
        Assert.AreEqual(30, rect.Right);
        Assert.AreEqual(40, rect.Bottom);
        Assert.AreEqual(default, BbScanRect.FromPoints([]));
    }

    [TestMethod]
    public void DiagnosticsDescribeTheFrameThatWasActuallyDecoded()
    {
        using var qr = Qr("DIAG");
        using var frame = ImageTransforms.Frame(qr, 640, 480);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.QrCode,
            Region = BbScanRegion.CenteredSquare(0.5),
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        decoder.Decode(frame.View, suppressDuplicates: false);
        var diagnostics = decoder.LastFrame;

        // Half the shorter edge, square: 240 by 240 of a 640 by 480 frame.
        Assert.AreEqual(240, diagnostics.DecodedWidth);
        Assert.AreEqual(240, diagnostics.DecodedHeight);
        Assert.IsTrue(diagnostics.Total > TimeSpan.Zero);
        Assert.IsTrue(diagnostics.EquivalentFramesPerSecond > 0);
        Assert.AreEqual(1, decoder.FramesDecoded);
    }

    [TestMethod]
    public void ContinuousScanningOfEmptyFramesDoesNotAllocate()
    {
        // This is the property that keeps a WebAssembly scanner free of collection pauses: the
        // common case, a frame the user has not yet aimed, must cost nothing but time.
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.AllLinear,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        using var blank = BbLuminanceBuffer.Rent(320, 240);
        blank.Pixels.Fill(255);

        for (var i = 0; i < 10; i++)
        {
            decoder.Decode(blank.View, suppressDuplicates: false);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 50; i++)
        {
            decoder.Decode(blank.View, suppressDuplicates: false);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsTrue(allocated == 0, $"Fifty empty frames allocated {allocated} bytes on the linear path.");
    }

    [TestMethod]
    public void ContinuousScanningWithEveryFormatStaysWithinAFixedOverhead()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        using var blank = BbLuminanceBuffer.Rent(320, 240);
        blank.Pixels.Fill(255);

        for (var i = 0; i < 10; i++)
        {
            decoder.Decode(blank.View, suppressDuplicates: false);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 50; i++)
        {
            decoder.Decode(blank.View, suppressDuplicates: false);
        }

        var allocated = (GC.GetAllocatedBytesForCurrentThread() - before) / 50;

        // The matrix path still allocates the bit plane wrapper for the frame, but nothing that
        // scales with the work done: detectors, finders and their callbacks are all reused.
        Assert.IsTrue(allocated <= 128, $"Each empty frame allocated {allocated} bytes with every format enabled.");
    }

    [TestMethod]
    public void SeparateDecodersDoNotShareState()
    {
        // The decoder is documented as one per session, so two of them must be independent even
        // when used from different threads at the same time.
        using var image = Qr("PARALLEL");
        var pixels = image.Pixels.ToArray();
        var width = image.Width;
        var height = image.Height;

        var results = new string?[8];
        Parallel.For(0, results.Length, i =>
        {
            using var decoder = new BbBarcodeDecoder(new BbScannerOptions
            {
                Formats = BbBarcodeFormat.QrCode,
                DuplicateSuppressionWindow = TimeSpan.Zero,
            });

            results[i] = decoder.Decode(pixels, width, height, BbPixelFormat.Gray8, false)?.Text;
        });

        foreach (var r in results) { Assert.AreEqual("PARALLEL", r); };
    }

    [TestMethod]
    public void EnablingPdf417CostsNothingWhileTheTableIsAPlaceholder()
    {
        // The shipped symbol character table is generated, not the specification's, so the reader
        // is deliberately not created; the format must simply find nothing rather than misread.
        Assert.IsFalse(BbPdf417SymbolTable.Current.IsSpecificationTable);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.Pdf417,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        using var image = Qr("NOT PDF417");
        Assert.IsNull(decoder.Decode(image.View, suppressDuplicates: false));
    }

    [TestMethod]
    public void EveryFormatFlagIsDistinctAndTheGroupsAgree()
    {
        var all = Enum.GetValues<BbBarcodeFormat>()
            .Where(f => f is not (BbBarcodeFormat.None or BbBarcodeFormat.All or BbBarcodeFormat.AllLinear or BbBarcodeFormat.AllMatrix))
            .ToArray();

        Assert.AreEqual(all.Length, all.Distinct().Count());

        // Every symbology belongs to exactly one of the two groups.
        var linear = all.Where(f => BbBarcodeFormat.AllLinear.HasFlag(f)).ToArray();
        var matrix = all.Where(f => BbBarcodeFormat.AllMatrix.HasFlag(f)).ToArray();
        Assert.AreEqual(all.Length, linear.Length + matrix.Length);
        Assert.IsEmpty(linear.Intersect(matrix));

        var union = BbBarcodeFormat.None;
        foreach (var format in all)
        {
            union |= format;
        }

        Assert.AreEqual(BbBarcodeFormat.All, union);
    }

    [TestMethod]
    public void BrowserErrorsAreClassifiedByTheirDomExceptionName()
    {
        var cases = new (string Message, BbScannerErrorKind Kind)[]
        {
            ("NotAllowedError: permission dismissed", BbScannerErrorKind.PermissionDenied),
            ("SecurityError", BbScannerErrorKind.PermissionDenied),
            ("NotFoundError: no device", BbScannerErrorKind.NoCamera),
            ("NotReadableError: could not start", BbScannerErrorKind.CameraUnavailable),
            ("OverconstrainedError: deviceId", BbScannerErrorKind.ConstraintsUnsatisfied),
            ("NotSupportedError", BbScannerErrorKind.NotSupported),
            ("InvalidStateError: the scanner video element is not in the document.", BbScannerErrorKind.Unknown),
            ("something else entirely", BbScannerErrorKind.Unknown),
        };

        foreach (var (message, kind) in cases)
        {
            var exception = BbScannerException.FromBrowserError(new InvalidOperationException(message));
            Assert.AreEqual(kind, exception.Kind);
            Assert.IsNotEmpty(exception.Message);
            Assert.IsNotNull(exception.InnerException);
        }

        Assert.ThrowsExactly<ArgumentNullException>(() => BbScannerException.FromBrowserError(null!));
    }

    [TestMethod]
    public void AMissingVideoElementIsNotReportedAsAMissingCamera()
    {
        var exception = BbScannerException.FromBrowserError(
            new InvalidOperationException("InvalidStateError: the scanner video element is not in the document."));

        Assert.AreNotEqual(BbScannerErrorKind.NoCamera, exception.Kind);
        Assert.DoesNotContain("camera matching", exception.Message);
        Assert.Contains("page", exception.Message);
    }
}
