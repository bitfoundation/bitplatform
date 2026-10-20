namespace Bit.BbScanner.Tests;

/// <summary>
/// Decodes symbols through the transforms a camera actually applies: rotation, perspective,
/// blur, sensor noise, uneven lighting, low contrast, scaling and a cluttered background.
/// </summary>
/// <remarks>
/// A round trip through a clean, axis aligned render proves the tables are right. It does not
/// prove the detector works, because detection is exactly the stage that a real frame breaks.
/// These cases are the ones that failed before the detectors were rebuilt, so they are the
/// regression net for them.
/// </remarks>
[TestClass]
public class RealWorldImageTests
{
    private const string QrPayload = "https://example.com/p/12345";
    private const string DataMatrixPayload = "DM-2026-TEST";
    private const string Code128Payload = "AB-1234";
    private const string Ean13Payload = "4006381333931";

    private static BbLuminanceBuffer Qr(int scale = 4) =>
        SyntheticImage.FromMatrix(QrEncoder.Encode(QrPayload, BbQrErrorCorrectionLevel.M, 2), scale);

    private static BbLuminanceBuffer DataMatrix(int scale = 4) =>
        SyntheticImage.FromMatrix(DataMatrixEncoder.Encode(DataMatrixPayload), scale);

    private static BbLuminanceBuffer Code128(int scale = 3) =>
        SyntheticImage.FromLinear(LinearEncoders.Code128(Code128Payload), scale, 80);

    private static BbLuminanceBuffer Ean13(int scale = 3) =>
        SyntheticImage.FromLinear(LinearEncoders.Ean13(Ean13Payload), scale, 80);

    private static string? Decode(BbLuminanceBuffer frame, BbBarcodeFormat formats, bool stillImage = false)
    {
        var options = stillImage ? BbScannerOptions.ForStillImages() : new BbScannerOptions();
        options.Formats = formats;
        options.DuplicateSuppressionWindow = TimeSpan.Zero;

        using var decoder = new BbBarcodeDecoder(options);
        return decoder.Decode(frame.View, suppressDuplicates: false)?.Text;
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(15)]
    [DataRow(30)]
    [DataRow(45)]
    [DataRow(60)]
    [DataRow(90)]
    [DataRow(135)]
    [DataRow(180)]
    [DataRow(270)]
    public void QrCodeDecodesAtAnyRotation(double degrees)
    {
        using var symbol = Qr();
        using var rotated = ImageTransforms.Rotate(symbol, degrees);
        using var frame = ImageTransforms.Frame(rotated, 640, 480);

        Assert.AreEqual(QrPayload, Decode(frame, BbBarcodeFormat.QrCode));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(15)]
    [DataRow(30)]
    [DataRow(45)]
    [DataRow(90)]
    [DataRow(135)]
    [DataRow(180)]
    public void DataMatrixDecodesAtAnyRotation(double degrees)
    {
        using var symbol = DataMatrix();
        using var rotated = ImageTransforms.Rotate(symbol, degrees);
        using var frame = ImageTransforms.Frame(rotated, 640, 480);

        Assert.AreEqual(DataMatrixPayload, Decode(frame, BbBarcodeFormat.DataMatrix));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(15)]
    [DataRow(30)]
    [DataRow(180)]
    public void LinearSymbolsDecodeAtTheRotationsAHandProduces(double degrees)
    {
        using var symbol = Code128();
        using var rotated = ImageTransforms.Rotate(symbol, degrees);
        using var frame = ImageTransforms.Frame(rotated, 640, 480);

        Assert.AreEqual(Code128Payload, Decode(frame, BbBarcodeFormat.Code128));
    }

    [TestMethod]
    public void LinearSymbolsDecodeWhenPresentedVertically()
    {
        using var symbol = Ean13();
        using var rotated = ImageTransforms.Rotate(symbol, 90);
        using var frame = ImageTransforms.Frame(rotated, 640, 480);

        Assert.AreEqual(Ean13Payload, Decode(frame, BbBarcodeFormat.Ean13));
    }

    [TestMethod]
    public void TurningOffVerticalScanningGivesUpRotatedSymbols()
    {
        using var symbol = Ean13();
        using var rotated = ImageTransforms.Rotate(symbol, 90);
        using var frame = ImageTransforms.Frame(rotated, 640, 480);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.Ean13,
            TryVerticalLines = false,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        Assert.IsNull(decoder.Decode(frame.View, suppressDuplicates: false));
    }

    [TestMethod]
    [DataRow(15)]
    [DataRow(30)]
    public void MatrixSymbolsSurvivePerspective(double degrees)
    {
        using var qr = Qr();
        using var tiltedQr = ImageTransforms.Tilt(qr, degrees);
        using var qrFrame = ImageTransforms.Frame(tiltedQr, 640, 480);
        Assert.AreEqual(QrPayload, Decode(qrFrame, BbBarcodeFormat.QrCode));

        using var dm = DataMatrix();
        using var tiltedDm = ImageTransforms.Tilt(dm, degrees);
        using var dmFrame = ImageTransforms.Frame(tiltedDm, 640, 480);
        Assert.AreEqual(DataMatrixPayload, Decode(dmFrame, BbBarcodeFormat.DataMatrix));
    }

    [TestMethod]
    [DataRow(30)]
    [DataRow(60)]
    public void SensorNoiseDoesNotPreventDecoding(int amplitude)
    {
        using var qr = Qr();
        using var qrFrame = ImageTransforms.Frame(qr, 640, 480);
        SyntheticImage.AddNoise(qrFrame, amplitude, seed: 7);
        Assert.AreEqual(QrPayload, Decode(qrFrame, BbBarcodeFormat.QrCode));

        using var dm = DataMatrix();
        using var dmFrame = ImageTransforms.Frame(dm, 640, 480);
        SyntheticImage.AddNoise(dmFrame, amplitude, seed: 7);
        Assert.AreEqual(DataMatrixPayload, Decode(dmFrame, BbBarcodeFormat.DataMatrix));
    }

    [TestMethod]
    public void NoiseDoesNotShredBlankPaperIntoInk()
    {
        // A blank block thresholded against its own mean comes out half black, and a frame of
        // speckle costs an order of magnitude more time and finds nothing. This is what the
        // binariser's noise floor estimate exists to prevent.
        using var frame = BbLuminanceBuffer.Rent(640, 480);
        frame.Pixels.Fill(220);
        SyntheticImage.AddNoise(frame, amplitude: 25, seed: 11);

        using var binary = new BbHybridBinarizer().GetBlackMatrix(frame.View);
        Assert.IsNotNull(binary);

        var ink = 0;
        for (var y = 0; y < binary.Height; y++)
        {
            for (var x = 0; x < binary.Width; x++)
            {
                if (binary[x, y])
                {
                    ink++;
                }
            }
        }

        var fraction = (double)ink / (binary.Width * binary.Height);
        Assert.IsTrue(fraction < 0.05, $"Blank noisy paper binarised to {fraction:P1} ink.");
    }

    [TestMethod]
    [DataRow(0.6)]
    [DataRow(1.0)]
    [DataRow(1.4)]
    public void BlurDoesNotPreventDecoding(double sigma)
    {
        using var qr = Qr();
        using var blurredQr = ImageTransforms.Blur(qr, sigma);
        using var qrFrame = ImageTransforms.Frame(blurredQr, 640, 480);
        Assert.AreEqual(QrPayload, Decode(qrFrame, BbBarcodeFormat.QrCode));

        using var dm = DataMatrix();
        using var blurredDm = ImageTransforms.Blur(dm, sigma);
        using var dmFrame = ImageTransforms.Frame(blurredDm, 640, 480);
        Assert.AreEqual(DataMatrixPayload, Decode(dmFrame, BbBarcodeFormat.DataMatrix));
    }

    [TestMethod]
    public void UnevenLightingDoesNotPreventDecoding()
    {
        foreach (var (left, right) in new[] { (0.35, 1.0), (1.0, 0.3) })
        {
            using var qr = Qr();
            using var frame = ImageTransforms.Frame(qr, 640, 480);
            using var lit = ImageTransforms.LightingGradient(frame, left, right);
            Assert.AreEqual(QrPayload, Decode(lit, BbBarcodeFormat.QrCode));

            using var dm = DataMatrix();
            using var dmFrame = ImageTransforms.Frame(dm, 640, 480);
            using var dmLit = ImageTransforms.LightingGradient(dmFrame, left, right);
            Assert.AreEqual(DataMatrixPayload, Decode(dmLit, BbBarcodeFormat.DataMatrix));

            using var ean = Ean13();
            using var eanFrame = ImageTransforms.Frame(ean, 640, 480);
            using var eanLit = ImageTransforms.LightingGradient(eanFrame, left, right);
            Assert.AreEqual(Ean13Payload, Decode(eanLit, BbBarcodeFormat.Ean13));
        }
    }

    [TestMethod]
    [DataRow((byte)90, (byte)160)]
    [DataRow((byte)10, (byte)70)]
    public void LowContrastAndUnderexposureStillDecode(byte dark, byte light)
    {
        using var qr = Qr();
        using var faded = ImageTransforms.Contrast(qr, dark, light);
        using var frame = ImageTransforms.Frame(faded, 640, 480, light);

        Assert.AreEqual(QrPayload, Decode(frame, BbBarcodeFormat.QrCode));
    }

    [TestMethod]
    public void InvertedSymbolsDecodeWithTheStillImageSettings()
    {
        using var qr = Qr();
        using var inverted = ImageTransforms.Invert(qr);
        using var frame = ImageTransforms.Frame(inverted, 640, 480, background: 0);
        Assert.AreEqual(QrPayload, Decode(frame, BbBarcodeFormat.QrCode, stillImage: true));

        using var dm = DataMatrix();
        using var invertedDm = ImageTransforms.Invert(dm);
        using var dmFrame = ImageTransforms.Frame(invertedDm, 640, 480, background: 0);
        Assert.AreEqual(DataMatrixPayload, Decode(dmFrame, BbBarcodeFormat.DataMatrix, stillImage: true));

        using var c128 = Code128();
        using var invertedLinear = ImageTransforms.Invert(c128);
        using var linearFrame = ImageTransforms.Frame(invertedLinear, 640, 480, background: 0);
        Assert.AreEqual(Code128Payload, Decode(linearFrame, BbBarcodeFormat.Code128, stillImage: true));
    }

    [TestMethod]
    public void LiveSettingsDoNotSpendTimeOnInvertedSymbols()
    {
        // Inverted retries double the cost of every frame that finds nothing, which is most of
        // them, so they are deliberately off for live scanning.
        using var qr = Qr();
        using var inverted = ImageTransforms.Invert(qr);
        using var frame = ImageTransforms.Frame(inverted, 640, 480, background: 0);

        Assert.IsNull(Decode(frame, BbBarcodeFormat.QrCode));
    }

    [TestMethod]
    [DataRow(0.4)]
    [DataRow(0.6)]
    [DataRow(1.5)]
    public void SymbolsDecodeAtAnyDistance(double factor)
    {
        using var qr = Qr(6);
        using var scaled = ImageTransforms.Scale(qr, factor);
        using var frame = ImageTransforms.Frame(scaled, 640, 480);

        Assert.AreEqual(QrPayload, Decode(frame, BbBarcodeFormat.QrCode));
    }

    [TestMethod]
    public void SymbolsAwayFromTheCentreAreStillFound()
    {
        foreach (var (x, y) in new[] { (20, 20), (380, 260), (20, 260), (380, 20) })
        {
            using var qr = Qr(3);
            using var frame = BbLuminanceBuffer.Rent(640, 480);
            frame.Pixels.Fill(255);
            ImageTransforms.Paste(qr, frame, x, y);
            Assert.AreEqual(QrPayload, Decode(frame, BbBarcodeFormat.QrCode));

            using var dm = DataMatrix(4);
            using var dmFrame = BbLuminanceBuffer.Rent(640, 480);
            dmFrame.Pixels.Fill(255);
            ImageTransforms.Paste(dm, dmFrame, x, y);
            Assert.AreEqual(DataMatrixPayload, Decode(dmFrame, BbBarcodeFormat.DataMatrix));
        }
    }

    [TestMethod]
    public void ABusyBackgroundDoesNotHideTheSymbol()
    {
        using var qr = Qr();
        using var frame = ImageTransforms.Frame(qr, 640, 480);
        ImageTransforms.Clutter(frame, seed: 3, (640 - qr.Width) / 2, (480 - qr.Height) / 2, qr.Width, qr.Height);

        Assert.AreEqual(QrPayload, Decode(frame, BbBarcodeFormat.QrCode));
    }

    [TestMethod]
    public void ACombinationOfEverydayDefectsStillDecodes()
    {
        // Slightly tilted, slightly out of focus, unevenly lit and noisy: an ordinary photograph.
        foreach (var (symbol, format, expected) in new (Func<BbLuminanceBuffer>, BbBarcodeFormat, string)[]
                 {
                     (() => Qr(), BbBarcodeFormat.QrCode, QrPayload),
                     (() => DataMatrix(), BbBarcodeFormat.DataMatrix, DataMatrixPayload),
                 })
        {
            using var image = symbol();
            using var rotated = ImageTransforms.Rotate(image, 10);
            using var blurred = ImageTransforms.Blur(rotated, 1.0);
            using var framed = ImageTransforms.Frame(blurred, 640, 480);
            using var lit = ImageTransforms.LightingGradient(framed, 0.6, 1.0);
            SyntheticImage.AddNoise(lit, amplitude: 20, seed: 11);

            Assert.AreEqual(expected, Decode(lit, format));
        }
    }

    [TestMethod]
    public void ColourFramesDecodeThroughEveryPixelLayout()
    {
        using var qr = Qr();
        using var frame = ImageTransforms.Frame(qr, 640, 480);
        var rgba = ImageTransforms.ToRgba(frame);

        var bgra = new byte[rgba.Length];
        for (var i = 0; i < rgba.Length; i += 4)
        {
            bgra[i] = rgba[i + 2];
            bgra[i + 1] = rgba[i + 1];
            bgra[i + 2] = rgba[i];
            bgra[i + 3] = rgba[i + 3];
        }

        var rgb = new byte[frame.Width * frame.Height * 3];
        for (var i = 0; i < frame.Width * frame.Height; i++)
        {
            rgb[(i * 3) + 0] = rgba[(i * 4) + 0];
            rgb[(i * 3) + 1] = rgba[(i * 4) + 1];
            rgb[(i * 3) + 2] = rgba[(i * 4) + 2];
        }

        var options = new BbScannerOptions
        {
            Formats = BbBarcodeFormat.QrCode,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        };

        using var decoder = new BbBarcodeDecoder(options);
        Assert.AreEqual(QrPayload, decoder.Decode(rgba, frame.Width, frame.Height, BbPixelFormat.Rgba32, false)?.Text);
        Assert.AreEqual(QrPayload, decoder.Decode(bgra, frame.Width, frame.Height, BbPixelFormat.Bgra32, false)?.Text);
        Assert.AreEqual(QrPayload, decoder.Decode(rgb, frame.Width, frame.Height, BbPixelFormat.Rgb24, false)?.Text);
        Assert.AreEqual(QrPayload, decoder.Decode(frame.Pixels.ToArray(), frame.Width, frame.Height, BbPixelFormat.Gray8, false)?.Text);
    }

    [TestMethod]
    public void EveryFrameShapeSurvivesArbitraryContent()
    {
        // The binariser, the region finder and the line sweep all derive their geometry from the
        // frame's dimensions. Extreme shapes are where an off-by-one in that arithmetic shows up,
        // and a camera that hands back an unusual size must not crash the scanner.
        var random = new Random(2026);
        var options = BbScannerOptions.ForStillImages();
        options.DuplicateSuppressionWindow = TimeSpan.Zero;

        (int Width, int Height)[] shapes =
        [
            (8, 8), (9, 9), (16, 16), (31, 17), (33, 33), (40, 40), (41, 39),
            (64, 8), (8, 64), (640, 9), (9, 640), (127, 3), (3, 127), (320, 240), (65, 65),
        ];

        foreach (var (width, height) in shapes)
        {
            using var decoder = new BbBarcodeDecoder(options);
            using var frame = BbLuminanceBuffer.Rent(width, height);

            for (var attempt = 0; attempt < 12; attempt++)
            {
                switch (attempt % 4)
                {
                    case 0:
                        frame.Pixels.Fill(0);
                        break;
                    case 1:
                        frame.Pixels.Fill(255);
                        break;
                    case 2:
                        random.NextBytes(frame.Array.AsSpan(0, width * height));
                        break;
                    default:
                        for (var i = 0; i < frame.Pixels.Length; i++)
                        {
                            frame.Pixels[i] = (byte)((i % 2) * 255);
                        }

                        break;
                }

                // Anything at all may come back, including null; nothing may throw.
                if (width >= 8 && height >= 8)
                {
                    decoder.Decode(frame.View, suppressDuplicates: false);
                }
            }
        }
    }

    [TestMethod]
    public void ContinuousScanningOfChangingFrameSizesDoesNotCorruptState()
    {
        // A camera that renegotiates its resolution mid-stream hands the same decoder frames of
        // different shapes, so every cached buffer has to grow and shrink safely.
        var options = new BbScannerOptions { DuplicateSuppressionWindow = TimeSpan.Zero };
        using var decoder = new BbBarcodeDecoder(options);

        foreach (var (width, height) in new[] { (320, 240), (640, 480), (240, 320), (1280, 720), (64, 48), (640, 480) })
        {
            using var qr = Qr(Math.Max(2, Math.Min(width, height) / 40));
            using var frame = BbLuminanceBuffer.Rent(width, height);
            frame.Pixels.Fill(255);
            ImageTransforms.Paste(qr, frame, Math.Max(0, (width - qr.Width) / 2), Math.Max(0, (height - qr.Height) / 2));

            var result = decoder.Decode(frame.View, suppressDuplicates: false);
            if (qr.Width <= width && qr.Height <= height)
            {
                Assert.AreEqual(QrPayload, result?.Text);
            }
        }
    }

    [TestMethod]
    public void RandomFramesNeverProduceAResultAndNeverThrow()
    {
        // Continuous scanning runs the whole pipeline on hundreds of frames of nothing. A single
        // accepted frame is a wrong value; a single exception kills the scanner.
        var random = new Random(5);
        var options = BbScannerOptions.ForStillImages();
        options.DuplicateSuppressionWindow = TimeSpan.Zero;

        using var decoder = new BbBarcodeDecoder(options);
        using var frame = BbLuminanceBuffer.Rent(320, 240);

        for (var attempt = 0; attempt < 200; attempt++)
        {
            switch (attempt % 4)
            {
                case 0:
                    random.NextBytes(frame.Array.AsSpan(0, 320 * 240));
                    break;
                case 1:
                    for (var i = 0; i < frame.Pixels.Length; i++)
                    {
                        frame.Pixels[i] = (byte)(random.Next(2) == 0 ? 0 : 255);
                    }

                    break;
                case 2:
                    frame.Pixels.Fill(255);
                    ImageTransforms.Clutter(frame, attempt, 0, 0, 0, 0);
                    break;
                default:
                    frame.Pixels.Fill((byte)random.Next(256));
                    break;
            }

            Assert.IsNull(decoder.Decode(frame.View, suppressDuplicates: false));
        }
    }
}
