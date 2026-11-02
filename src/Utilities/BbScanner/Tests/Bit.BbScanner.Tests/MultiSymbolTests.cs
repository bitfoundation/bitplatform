namespace Bit.BbScanner.Tests;

/// <summary>
/// <see cref="BbBarcodeDecoder.DecodeAll(in BbLuminanceView, int, bool)"/>: every symbol in a frame rather than the
/// first - a shelf label carrying a QR code and an EAN-13, a sheet of tickets, a carton with several labels.
/// </summary>
[TestClass]
public class MultiSymbolTests
{
    [TestMethod]
    public void FindsEveryQrCodeInAFrame()
    {
        using var frame = Frame(960, 640,
            (Qr("FIRST"), 60, 60),
            (Qr("SECOND"), 560, 80),
            (Qr("THIRD"), 300, 360));

        var results = DecodeAll(frame, BbBarcodeFormat.QrCode);

        CollectionAssert.AreEquivalent(new[] { "FIRST", "SECOND", "THIRD" }, results.Select(r => r.Text).ToArray());
        Assert.IsTrue(results.All(r => r.Format == BbBarcodeFormat.QrCode));
    }

    [TestMethod]
    public void FindsMatrixAndLinearSymbolsTogether()
    {
        using var frame = Frame(1000, 600,
            (Qr("https://example.com/item/42"), 40, 40),
            (DataMatrix("LOT-2026-7"), 600, 60),
            (Linear(LinearEncoders.Ean13("4006381333931")), 40, 380),
            (Linear(LinearEncoders.Code128("SHIP-0042")), 520, 380));

        var results = DecodeAll(frame, BbBarcodeFormat.All);

        CollectionAssert.AreEquivalent(
            new[] { "https://example.com/item/42", "LOT-2026-7", "4006381333931", "SHIP-0042" },
            results.Select(r => r.Text).ToArray());
    }

    [TestMethod]
    public void TwoCopiesOfTheSameCodeInDifferentPlacesAreTwoResults()
    {
        using var frame = Frame(900, 400, (Qr("SAME"), 40, 60), (Qr("SAME"), 560, 60));

        var results = DecodeAll(frame, BbBarcodeFormat.QrCode);

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(r => r.Text == "SAME"));
        var centres = results.Select(r => r.BoundingBox.Center.X).Order().ToArray();
        Assert.IsGreaterThan(300, centres[1] - centres[0], "The two results are the two symbols, not one read twice.");
    }

    [TestMethod]
    public void EachResultCarriesItsOwnCorners()
    {
        using var frame = Frame(900, 400, (Qr("LEFT"), 40, 60), (Qr("RIGHT"), 560, 60));

        var results = DecodeAll(frame, BbBarcodeFormat.QrCode);

        var left = results.Single(r => r.Text == "LEFT");
        var right = results.Single(r => r.Text == "RIGHT");
        Assert.IsLessThan(450, left.BoundingBox.Right);
        Assert.IsGreaterThan(450, right.BoundingBox.X);
    }

    [TestMethod]
    public void StopsAtTheRequestedNumberOfSymbols()
    {
        using var frame = Frame(960, 640, (Qr("A1"), 60, 60), (Qr("B2"), 560, 80), (Qr("C3"), 300, 360));

        var results = DecodeAll(frame, BbBarcodeFormat.QrCode, maxSymbols: 2);

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void AFrameWithNothingInItReturnsAnEmptyList()
    {
        using var frame = Frame(640, 480);

        Assert.IsEmpty(DecodeAll(frame, BbBarcodeFormat.All));
    }

    [TestMethod]
    public void ASingleSymbolReadsTheSameAsDecode()
    {
        using var frame = Frame(640, 480, (Qr("ONLY-ONE"), 200, 100));
        using var decoder = new BbBarcodeDecoder(Options(BbBarcodeFormat.All));

        var single = decoder.Decode(frame.View, suppressDuplicates: false);
        var all = decoder.DecodeAll(frame.View, suppressDuplicates: false);

        Assert.IsNotNull(single);
        Assert.HasCount(1, all);
        Assert.AreEqual(single.Text, all[0].Text);
        Assert.AreEqual(single.BoundingBox, all[0].BoundingBox);
    }

    [TestMethod]
    public void DuplicateSuppressionAppliesToEachSymbol()
    {
        using var frame = Frame(900, 400, (Qr("KEPT"), 40, 60), (Qr("ALSO-KEPT"), 560, 60));
        var options = Options(BbBarcodeFormat.QrCode);
        options.DuplicateSuppressionWindow = TimeSpan.FromMinutes(1);
        using var decoder = new BbBarcodeDecoder(options);

        Assert.HasCount(2, decoder.DecodeAll(frame.View));
        Assert.IsEmpty(decoder.DecodeAll(frame.View), "Both were reported a moment ago.");
    }

    [TestMethod]
    public void AScanRegionLimitsTheSearch()
    {
        using var frame = Frame(960, 480, (Qr("INSIDE"), 60, 100), (Qr("OUTSIDE"), 620, 100));
        var options = Options(BbBarcodeFormat.QrCode);
        options.Region = new BbScanRegion(0, 0, 0.5, 1);
        using var decoder = new BbBarcodeDecoder(options);

        var results = decoder.DecodeAll(frame.View, suppressDuplicates: false);

        Assert.AreEqual("INSIDE", results.Single().Text);
    }

    [TestMethod]
    public void PackedPixelsWorkTheSameAsALuminanceView()
    {
        using var frame = Frame(900, 400, (Qr("RGBA-1"), 40, 60), (Qr("RGBA-2"), 560, 60));
        var rgba = ImageTransforms.ToRgba(frame, tint: false);
        using var decoder = new BbBarcodeDecoder(Options(BbBarcodeFormat.QrCode));

        var results = decoder.DecodeAll(rgba, frame.Width, frame.Height, BbPixelFormat.Rgba32, suppressDuplicates: false);

        CollectionAssert.AreEquivalent(new[] { "RGBA-1", "RGBA-2" }, results.Select(r => r.Text).ToArray());
    }

    [TestMethod]
    public void ZeroSymbolsIsRejected()
    {
        using var frame = Frame(640, 480);
        using var decoder = new BbBarcodeDecoder();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => decoder.DecodeAll(frame.View, maxSymbols: 0));
    }

    private static IReadOnlyList<BbBarcodeResult> DecodeAll(BbLuminanceBuffer frame, BbBarcodeFormat formats, int maxSymbols = 16)
    {
        using var decoder = new BbBarcodeDecoder(Options(formats));
        return decoder.DecodeAll(frame.View, maxSymbols, suppressDuplicates: false);
    }

    private static BbScannerOptions Options(BbBarcodeFormat formats)
    {
        var options = BbScannerOptions.ForStillImages();
        options.Formats = formats;
        return options;
    }

    private static BbLuminanceBuffer Qr(string text) => SyntheticImage.FromMatrix(QrEncoder.Encode(text), scale: 5);

    private static BbLuminanceBuffer DataMatrix(string text) => SyntheticImage.FromMatrix(DataMatrixEncoder.Encode(text), scale: 6);

    private static BbLuminanceBuffer Linear(bool[] modules) => SyntheticImage.FromLinear(modules, scale: 3, height: 160);

    /// <summary>A white frame with each symbol pasted at its position; the symbols are disposed once pasted.</summary>
    private static BbLuminanceBuffer Frame(int width, int height, params (BbLuminanceBuffer Symbol, int X, int Y)[] symbols)
    {
        var frame = BbLuminanceBuffer.Rent(width, height);
        frame.Pixels.Fill(255);
        foreach (var (symbol, x, y) in symbols)
        {
            ImageTransforms.Paste(symbol, frame, x, y);
            symbol.Dispose();
        }

        return frame;
    }
}
