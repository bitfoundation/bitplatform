namespace Bit.BbScanner.Tests;

/// <summary>
/// Round trips generated symbols through the full decoding path, from module grid to payload
/// and from rendered image to payload.
/// </summary>
[TestClass]
public class QrCodeDecoderTests
{
    [TestMethod]
    [DataRow("HELLO WORLD")]
    [DataRow("12345678901234567890")]
    [DataRow("https://example.com/products/12345?utm_source=scanner")]
    [DataRow("a")]
    [DataRow("Grüße aus München")]
    public void DecodesGridForEveryMaskAndLevel(string content)
    {
        var decoder = new BbQrDecoder();

        foreach (var level in new[]
                 {
                     BbQrErrorCorrectionLevel.L,
                     BbQrErrorCorrectionLevel.M,
                     BbQrErrorCorrectionLevel.Q,
                     BbQrErrorCorrectionLevel.H,
                 })
        {
            for (var mask = 0; mask < 8; mask++)
            {
                using var matrix = QrEncoder.Encode(content, level, mask);
                var result = decoder.Decode(matrix);

                Assert.IsNotNull(result);
                Assert.AreEqual(content, result.Text);
                Assert.AreEqual(level.ToLetter(), result.ErrorCorrectionLevel);
            }
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(10)]
    [DataRow(27)]
    [DataRow(40)]
    public void DecodesEveryVersionBand(int minimumVersion)
    {
        var content = new string('A', 12);
        using var matrix = QrEncoder.Encode(content, BbQrErrorCorrectionLevel.M, maskPattern: 3, minimumVersion);
        Assert.AreEqual(17 + (4 * minimumVersion), matrix.Width);

        var result = new BbQrDecoder().Decode(matrix);

        Assert.IsNotNull(result);
        Assert.AreEqual(content, result.Text);
        Assert.AreEqual(minimumVersion.ToString(System.Globalization.CultureInfo.InvariantCulture), result.SymbolVersion);
    }

    [TestMethod]
    public void DecodesFromRenderedImage()
    {
        const string Content = "BLAZOR BARCODE SCANNER 2026";
        using var matrix = QrEncoder.Encode(Content, BbQrErrorCorrectionLevel.Q, maskPattern: 2);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 5);

        var binarizer = new BbHybridBinarizer();
        using var binary = binarizer.GetBlackMatrix(image.View);
        Assert.IsNotNull(binary);

        var result = new BbQrCodeReader().Decode(binary, BbMatrixDecodeOptions.Thorough);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
        Assert.AreEqual(BbBarcodeFormat.QrCode, result.Format);
        Assert.IsTrue(result.Points.Length >= 3);
    }

    [TestMethod]
    public void DecodesFromNoisyImage()
    {
        const string Content = "NOISE TOLERANCE CHECK";
        using var matrix = QrEncoder.Encode(Content, BbQrErrorCorrectionLevel.H, maskPattern: 5);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 6);
        SyntheticImage.AddNoise(image, amplitude: 40, seed: 1234);

        using var binary = new BbHybridBinarizer().GetBlackMatrix(image.View);
        Assert.IsNotNull(binary);

        var result = new BbQrCodeReader().Decode(binary, BbMatrixDecodeOptions.Thorough);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
    }

    [TestMethod]
    public void RecoversFromDamagedModules()
    {
        const string Content = "ERROR CORRECTION";
        using var matrix = QrEncoder.Encode(Content, BbQrErrorCorrectionLevel.H, maskPattern: 0);

        // Flip a small block of data modules, well inside the symbol so that no function pattern
        // is touched. Level H should absorb this comfortably.
        for (var y = 12; y < 16; y++)
        {
            for (var x = 12; x < 16; x++)
            {
                matrix[x, y] = !matrix[x, y];
            }
        }

        var result = new BbQrDecoder().Decode(matrix);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Text);
        Assert.IsTrue(result.ErrorsCorrected > 0);
    }

    [TestMethod]
    public void ReturnsNullForAnEmptyImage()
    {
        using var blank = new BbBitMatrix(64, 64);
        Assert.IsNull(new BbQrCodeReader().Decode(blank, BbMatrixDecodeOptions.Live));
    }

    /// <summary>
    /// Large symbols whose padding region forms lattices of 1:1:3:1:1 coincidences. Each of
    /// these once stopped the finder pattern search before it reached the real third corner,
    /// so the detector was handed a triple with a false centre and gave up.
    /// </summary>
    [TestMethod]
    [DataRow(35, BbQrErrorCorrectionLevel.Q, 6, 0)]
    [DataRow(28, BbQrErrorCorrectionLevel.L, 5, 180)]
    [DataRow(21, BbQrErrorCorrectionLevel.M, 5, 90)]
    [DataRow(21, BbQrErrorCorrectionLevel.M, 5, 270)]
    [DataRow(38, BbQrErrorCorrectionLevel.H, 0, 180)]
    [DataRow(34, BbQrErrorCorrectionLevel.M, 2, 270)]
    [DataRow(37, BbQrErrorCorrectionLevel.H, 7, 90)]
    [DataRow(1, BbQrErrorCorrectionLevel.Q, 2, 0)]
    public void DecodesSymbolsWhoseDataRegionMimicsFinderPatterns(
        int version, BbQrErrorCorrectionLevel level, int maskPattern, int rotation)
    {
        var content = $"VERSION {version} LEVEL {level}";
        using var matrix = QrEncoder.Encode(content, level, maskPattern, version);
        using var rotated = Rotate(matrix, rotation);

        foreach (var scale in new[] { 3, 4, 5 })
        {
            using var image = SyntheticImage.FromMatrix(rotated, scale);
            using var binary = new BbHybridBinarizer().GetBlackMatrix(image.View);
            Assert.IsNotNull(binary);

            foreach (var options in new[] { BbMatrixDecodeOptions.Live, BbMatrixDecodeOptions.Thorough })
            {
                var result = new BbQrCodeReader().Decode(binary, options);

                Assert.IsTrue(result is not null, $"No symbol found at scale {scale} with TryHarder={options.TryHarder}.");
                Assert.AreEqual(content, result.Payload.Text);
            }
        }
    }

    public static IEnumerable<object[]> EveryVersion
    {
        get
        {
            var data = new List<object[]>();
            for (var version = 1; version <= 40; version++)
            {
                data.Add([version]);
            }

            return data;
        }
    }

    [TestMethod]
    [DynamicData(nameof(EveryVersion))]
    public void DecodesEveryVersionInEveryOrientation(int version)
    {
        var content = $"VERSION {version} LEVEL M";
        using var matrix = QrEncoder.Encode(content, BbQrErrorCorrectionLevel.M, version % 8, version);
        var reader = new BbQrCodeReader();

        foreach (var rotation in new[] { 0, 90, 180, 270 })
        {
            using var rotated = Rotate(matrix, rotation);
            using var image = SyntheticImage.FromMatrix(rotated, scale: 4);
            using var binary = new BbHybridBinarizer().GetBlackMatrix(image.View);
            Assert.IsNotNull(binary);

            var result = reader.Decode(binary, BbMatrixDecodeOptions.Thorough);

            Assert.IsTrue(result is not null, $"Version {version} rotated {rotation} degrees was not found.");
            Assert.AreEqual(content, result.Payload.Text);
        }
    }

    [TestMethod]
    public void FinderRanksTheRealCornersFirstAndKeepsAlternatives()
    {
        // The upright version 35 symbol is the case where a lattice coincidence used to be taken
        // as the bottom-left corner. The best triple must now be the real one: an exact isosceles
        // right triangle with its centres 3.5 modules inside the symbol's corners.
        using var matrix = QrEncoder.Encode("VERSION 35 LEVEL Q", BbQrErrorCorrectionLevel.Q, maskPattern: 6, minimumVersion: 35);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
        using var binary = new BbHybridBinarizer().GetBlackMatrix(image.View);
        Assert.IsNotNull(binary);

        var finder = new BbQrFinderPatternFinder(binary);
        var candidates = finder.FindCandidates(tryHarder: true);

        Assert.IsTrue(candidates.Length >= 1);
        Assert.IsTrue(candidates.Length <= BbQrFinderPatternFinder.MaxCandidates);

        var best = candidates[0];
        const float Expected = (4 + 3.5f) * 4;
        const float Far = (4 + 157 - 3.5f) * 4;
        Assert.AreEqual(Expected, best.TopLeft.X, 1.0f);
        Assert.AreEqual(Expected, best.TopLeft.Y, 1.0f);
        Assert.AreEqual(Far, best.TopRight.X, 1.0f);
        Assert.AreEqual(Expected, best.TopRight.Y, 1.0f);
        Assert.AreEqual(Expected, best.BottomLeft.X, 1.0f);
        Assert.AreEqual(Far, best.BottomLeft.Y, 1.0f);

        for (var i = 1; i < candidates.Length; i++)
        {
            Assert.IsTrue(candidates[i].Distortion >= candidates[i - 1].Distortion);
        }

        Assert.IsTrue(finder.TryFind(tryHarder: true, out var patterns));
        Assert.AreEqual(3, patterns.Length);
        Assert.AreEqual(Far, patterns[0].Y, 1.0f);
    }

    private static BbBitMatrix Rotate(BbBitMatrix matrix, int degrees)
    {
        switch (degrees)
        {
            case 0:
                return matrix.Crop(0, 0, matrix.Width, matrix.Height);
            case 90:
                return matrix.Rotate90();
            case 180:
                return matrix.Rotate180();
            default:
                using (var half = matrix.Rotate180())
                {
                    return half.Rotate90();
                }
        }
    }
    [TestMethod]
    public void AnImageTiledWithFinderPatternsIsGivenUpOnQuickly()
    {
        // Choosing the finder patterns tries every triple of the centres found, so a picked file tiled with thousands of
        // them - which no photograph of real symbols is - cost the cube of their number: seven and a half minutes for
        // this one, about a second once the search keeps a few dozen. The bound only has to tell those apart, and a
        // shared build machine running every target framework's tests side by side takes ten times as long.
        const int Period = 8;
        const int Modules = 1500;
        using var tiles = new BbBitMatrix(Modules, Modules);
        for (var y = 0; y < Modules; y++)
        {
            for (var x = 0; x < Modules; x++)
            {
                var ring = Math.Max(Math.Abs((x % Period) - 3), Math.Abs((y % Period) - 3));
                tiles[x, y] = ring == 3 || ring <= 1;
            }
        }

        using var image = SyntheticImage.FromMatrix(tiles, 2, quietZoneModules: 0);
        var options = BbScannerOptions.ForStillImages();
        options.Formats = BbBarcodeFormat.QrCode;
        using var decoder = new BbBarcodeDecoder(options);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        decoder.Decode(image.View, suppressDuplicates: false);
        watch.Stop();

        Assert.IsLessThan(TimeSpan.FromSeconds(60), watch.Elapsed, $"Took {watch.Elapsed}.");
    }

    [TestMethod]
    public void AnImageOfTallStripedColumnsIsGivenUpOnQuickly()
    {
        // Every row of these columns reads 1:1:3:1:1, and the vertical check of each walked the whole black column before
        // its centre run was bounded: the square of the height. A hundred thousand rows took seven seconds, under the
        // bound a shared build machine needs; twice that many take thirty-five, and a tenth of a second bounded.
        using var stripes = new BbBitMatrix(64, 200_000);
        for (var y = 0; y < stripes.Height; y++)
        {
            for (var x = 0; x < stripes.Width; x++)
            {
                stripes[x, y] = (x % 8) is 0 or 2 or 3 or 4 or 6;
            }
        }

        var finder = new BbQrFinderPatternFinder(stripes);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var found = finder.FindCandidates(tryHarder: true).Length;
        watch.Stop();

        Assert.AreEqual(0, found);
        Assert.IsLessThan(TimeSpan.FromSeconds(10), watch.Elapsed, $"Took {watch.Elapsed}.");
    }

    [TestMethod]
    public void FinderPatternColumnsCrossedByLongBlackRowsAreGivenUpOnQuickly()
    {
        // Every candidate passes the vertical check and then meets an all black row in the horizontal one, which walked
        // the width of the image before its centre run was bounded: fourteen seconds or more, and a fifth of one bounded.
        using var bands = new BbBitMatrix(65_536, 1_024);
        for (var y = 0; y < bands.Height; y++)
        {
            for (var x = 0; x < bands.Width; x++)
            {
                bands[x, y] = (y % 8) switch
                {
                    2 or 4 => (x % 8) is 0 or 2 or 3 or 4 or 6,
                    3 => true,
                    0 or 6 => x % 8 == 3,
                    _ => false,
                };
            }
        }

        var finder = new BbQrFinderPatternFinder(bands);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var found = finder.FindCandidates(tryHarder: true).Length;
        watch.Stop();

        Assert.AreEqual(0, found);
        Assert.IsLessThan(TimeSpan.FromSeconds(10), watch.Elapsed, $"Took {watch.Elapsed}.");
    }

    [TestMethod]
    public void ACentreWhoseDiagonalIsFarLongerThanItsWidthIsRejected()
    {
        using var proportioned = Corners(stretch: 1);
        using var stretched = Corners(stretch: 5);

        Assert.AreEqual(1, new BbQrFinderPatternFinder(proportioned).FindCandidates(tryHarder: true).Length);
        Assert.AreEqual(0, new BbQrFinderPatternFinder(stretched).FindCandidates(tryHarder: true).Length);

        static BbBitMatrix Corners(int stretch)
        {
            var matrix = new BbBitMatrix(120, 120);
            foreach (var (x, y) in new[] { (30, 32), (70, 32), (30, 72) })
            {
                for (var t = -3; t <= 3; t++)
                {
                    matrix[x + t, y] = Math.Abs(t) != 2;
                    matrix[x, y + t] = Math.Abs(t) != 2;
                }

                var half = ((3 * stretch) - 1) / 2;
                for (var t = -half - (2 * stretch); t <= half + (2 * stretch); t++)
                {
                    matrix[x + t, y + t] = Math.Abs(t) <= half || Math.Abs(t) > half + stretch;
                }
            }

            return matrix;
        }
    }
}
