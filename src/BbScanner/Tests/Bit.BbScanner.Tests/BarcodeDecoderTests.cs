namespace Bit.BbScanner.Tests;

/// <summary>Exercises the end to end pipeline the way an application uses it.</summary>
[DoNotParallelize]
[TestClass]
public class BarcodeDecoderTests
{
    private static BbLuminanceBuffer RenderQr(string content) =>
        SyntheticImage.FromMatrix(QrEncoder.Encode(content), scale: 5);

    [TestMethod]
    public void DecodesAQrCodeFromAGrayscaleFrame()
    {
        using var decoder = new BbBarcodeDecoder();
        using var image = RenderQr("PIPELINE");

        var result = decoder.Decode(image.View);

        Assert.IsNotNull(result);
        Assert.AreEqual("PIPELINE", result.Text);
        Assert.AreEqual(BbBarcodeFormat.QrCode, result.Format);
        Assert.AreEqual(image.Width, result.FrameWidth);
        Assert.IsTrue(result.BoundingBox.Width > 0);
    }

    [TestMethod]
    public void DecodesALinearCodeFromAGrayscaleFrame()
    {
        using var decoder = new BbBarcodeDecoder();
        using var image = SyntheticImage.FromLinear(LinearEncoders.Code128("PIPELINE-128"), scale: 3, height: 120);

        var result = decoder.Decode(image.View);

        Assert.IsNotNull(result);
        Assert.AreEqual("PIPELINE-128", result.Text);
        Assert.AreEqual(BbBarcodeFormat.Code128, result.Format);
    }

    [TestMethod]
    public void DecodesFromPackedColourPixels()
    {
        using var image = RenderQr("RGBA FRAME");
        var rgba = new byte[image.Width * image.Height * 4];
        var pixels = image.Pixels;
        for (var i = 0; i < pixels.Length; i++)
        {
            rgba[(i * 4) + 0] = pixels[i];
            rgba[(i * 4) + 1] = pixels[i];
            rgba[(i * 4) + 2] = pixels[i];
            rgba[(i * 4) + 3] = 255;
        }

        using var decoder = new BbBarcodeDecoder();
        var result = decoder.Decode(rgba, image.Width, image.Height, BbPixelFormat.Rgba32);

        Assert.IsNotNull(result);
        Assert.AreEqual("RGBA FRAME", result.Text);
    }

    [TestMethod]
    public void SuppressesDuplicatesWithinTheWindow()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            DuplicateSuppressionWindow = TimeSpan.FromMinutes(5),
        });

        using var image = RenderQr("DUPLICATE");

        Assert.IsNotNull(decoder.Decode(image.View));
        Assert.IsNull(decoder.Decode(image.View));

        decoder.ResetDuplicateFilter();
        Assert.IsNotNull(decoder.Decode(image.View));
    }

    [TestMethod]
    public void DoesNotSuppressWhenTheWindowIsZero()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { DuplicateSuppressionWindow = TimeSpan.Zero });
        using var image = RenderQr("REPEATED");

        Assert.IsNotNull(decoder.Decode(image.View));
        Assert.IsNotNull(decoder.Decode(image.View));
    }

    [TestMethod]
    public void RestrictingFormatsSkipsOtherSymbologies()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.Ean13 });
        using var image = RenderQr("IGNORED");

        Assert.IsNull(decoder.Decode(image.View));
    }

    [TestMethod]
    public void ScanRegionRestrictsTheSearchAndPointsStayInFrameCoordinates()
    {
        using var qr = RenderQr("REGION");

        // Compose a frame twice as wide, with the symbol in the right half only.
        var width = qr.Width * 2;
        var height = qr.Height;
        using var frame = BbLuminanceBuffer.Rent(width, height);
        frame.Pixels.Fill(255);
        for (var y = 0; y < height; y++)
        {
            qr.Pixels.Slice(y * qr.Width, qr.Width).CopyTo(frame.Pixels.Slice((y * width) + qr.Width, qr.Width));
        }

        using var leftOnly = new BbBarcodeDecoder(new BbScannerOptions { Region = new BbScanRegion(0, 0, 0.5, 1) });
        Assert.IsNull(leftOnly.Decode(frame.View));

        using var rightOnly = new BbBarcodeDecoder(new BbScannerOptions { Region = new BbScanRegion(0.5, 0, 0.5, 1) });
        var result = rightOnly.Decode(frame.View);

        Assert.IsNotNull(result);
        Assert.AreEqual("REGION", result.Text);
        Assert.IsTrue(result.BoundingBox.X >= qr.Width, "Points must be reported in full frame coordinates.");
    }

    [TestMethod]
    public void DownsamplingStillDecodesAndReportsFullFrameCoordinates()
    {
        using var image = SyntheticImage.FromMatrix(QrEncoder.Encode("DOWNSAMPLED"), scale: 8);
        var gray = image.Pixels.ToArray();

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { DownsampleFactor = 2 });
        var result = decoder.Decode(gray, image.Width, image.Height, BbPixelFormat.Gray8);

        Assert.IsNotNull(result);
        Assert.AreEqual("DOWNSAMPLED", result.Text);
        Assert.IsTrue(result.BoundingBox.Width > image.Width / 4);
    }

    [TestMethod]
    public void APackedFrameIsCroppedToTheRegionBeforeItIsConverted()
    {
        using var qr = SyntheticImage.FromMatrix(QrEncoder.Encode("CROPPED FIRST"), scale: 4);
        using var frame = ImageTransforms.Frame(qr, 1280, 720);
        var rgba = ImageTransforms.ToRgba(frame, tint: false);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.QrCode,
            Region = BbScanRegion.CenteredSquare(0.5),
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        var result = decoder.Decode(rgba, frame.Width, frame.Height, BbPixelFormat.Rgba32);

        Assert.IsNotNull(result);
        Assert.AreEqual("CROPPED FIRST", result.Text);

        // Only the region was converted, and its corners still come back in whole frame coordinates.
        Assert.AreEqual(360, decoder.LastFrame.DecodedWidth);
        Assert.AreEqual(360, decoder.LastFrame.DecodedHeight);
        Assert.AreEqual(frame.Width / 2.0, result.BoundingBox.X + (result.BoundingBox.Width / 2), 8);
        Assert.AreEqual(frame.Height / 2.0, result.BoundingBox.Y + (result.BoundingBox.Height / 2), 8);
    }

    [TestMethod]
    public void ADownsampleFactorTooLargeForTheFrameIsLoweredRatherThanThrown()
    {
        // 60 / 8 leaves seven pixels a side, which used to be an exception on every frame.
        var gray = new byte[60 * 60];
        gray.AsSpan().Fill(255);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { DownsampleFactor = 8 });

        Assert.IsNull(decoder.Decode(gray, 60, 60, BbPixelFormat.Gray8));
        Assert.IsTrue(decoder.LastFrame.DecodedWidth >= 8, $"Decoded {decoder.LastFrame.DecodedWidth} pixels wide.");
        Assert.IsTrue(decoder.LastFrame.DecodedHeight >= 8, $"Decoded {decoder.LastFrame.DecodedHeight} pixels high.");
    }

    [TestMethod]
    public void ReportsDiagnosticsForEveryFrame()
    {
        using var decoder = new BbBarcodeDecoder();
        using var image = RenderQr("DIAGNOSTICS");

        decoder.Decode(image.View);

        var diagnostics = decoder.LastFrame;
        Assert.IsTrue(diagnostics.Decoded);
        Assert.AreEqual(image.Width, diagnostics.DecodedWidth);
        Assert.IsTrue(diagnostics.Total > TimeSpan.Zero);
        Assert.AreEqual(1, decoder.FramesDecoded);
        Assert.AreEqual(1, decoder.FramesWithSymbol);
    }

    [TestMethod]
    public void RepeatedFramesDoNotAllocateAfterWarmUp()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.QrCode,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        // A blank frame is the common case during continuous scanning: nothing is in view.
        using var blank = BbLuminanceBuffer.Rent(320, 240);
        blank.Pixels.Fill(255);

        for (var i = 0; i < 5; i++)
        {
            decoder.Decode(blank.View);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
        {
            decoder.Decode(blank.View);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // A small, fixed overhead is acceptable; growth proportional to frames is not.
        Assert.IsTrue(allocated < 20 * 1024, $"Allocated {allocated} bytes over 20 empty frames.");
    }
}
