namespace Bit.BbScanner.Tests;

/// <summary>
/// MaxiCode and Micro QR: the test kit's encoders round tripped, reading at every angle down to the module sizes a
/// phone sees a parcel label or a small part at, and neither read where it is not. The reference fixtures are read as
/// written in <c>ReferenceSymbolTests</c>.
/// </summary>
[TestClass]
public class MaxiCodeAndMicroQrTests
{
    private const BbBarcodeFormat Both = BbBarcodeFormat.MaxiCode | BbBarcodeFormat.MicroQrCode;

    [TestMethod]
    [DataRow("UPPER CASE, DIGITS 0123 AND $%&'()*+-./:", 4)]
    [DataRow("lower case and Mixed Case {with} [brackets] @ ~", 4)]
    [DataRow("123456789 and 987654321012345678", 4)]
    [DataRow("Enhanced error correction: mode 5", 5)]
    public void MaxiCodeRoundTripsModesFourAndFive(string content, int mode)
    {
        using var matrix = MaxiCodeEncoder.Encode(content, mode);
        using var image = SyntheticImage.FromMaxiCode(matrix, moduleWidth: 4);

        var result = Decode(image, BbBarcodeFormat.MaxiCode, still: true);

        Assert.IsNotNull(result, content);
        Assert.AreEqual(content, result.Text);
        Assert.AreEqual(BbBarcodeFormat.MaxiCode, result.Format);
        Assert.AreEqual($"Mode {mode}", result.Metadata?.SymbolVersion);
        Assert.AreEqual(mode == 5 ? "Enhanced" : "Standard", result.Metadata?.ErrorCorrectionLevel);
    }

    [TestMethod]
    public void MicroQrRoundTripsEverySymbolAndMask()
    {
        // M1 to M4 at each of their levels, numeric, alphanumeric and bytes, with each of the four masks.
        (string Content, char Level, string Version)[] cases =
        [
            ("12345", 'L', "M1"),
            ("ABC12", 'L', "M2"), ("0123456", 'M', "M2"),
            ("bytes", 'L', "M3"), ("HELLO 42", 'M', "M3"),
            ("Micro QR v4!", 'L', "M4"), ("ABCDEFGHIJKLMN", 'M', "M4"), ("M4Q 9876", 'Q', "M4"),
        ];
        var failures = new List<string>();
        foreach (var (content, level, version) in cases)
        {
            for (var mask = 0; mask < 4; mask++)
            {
                using var matrix = MicroQrEncoder.Encode(content, level, mask);
                Assert.AreEqual(9 + (2 * (version[1] - '0')), matrix.Width, $"{content} {level}");
                using var image = SyntheticImage.FromMatrix(matrix, 3, quietZoneModules: 2);
                var result = Decode(image, BbBarcodeFormat.MicroQrCode, still: true);
                if (result?.Text != content || result.Format != BbBarcodeFormat.MicroQrCode || result.Metadata?.SymbolVersion != version)
                {
                    failures.Add($"{content} {level} mask {mask}: {result?.Text} {result?.Metadata?.SymbolVersion}");
                }
            }
        }

        Assert.IsEmpty(failures, string.Join("; ", failures));
    }

    [TestMethod]
    [DataRow("maxicode-mode2", 3)]
    [DataRow("maxicode-mode3", 4)]
    [DataRow("maxicode-mode4", 3)]
    [DataRow("maxicode-mode5", 3)]
    [DataRow("microqr-m1", 2)]
    [DataRow("microqr-m2-m", 2)]
    [DataRow("microqr-m3-m", 3)]
    [DataRow("microqr-m4-l", 2)]
    public void ReadsAtEveryAngleStillAndLiveWithinFourFrames(string id, int pixels)
    {
        // Pixels a module across: MaxiCode at three, a parcel label at arm's length in a 640 pixel frame, and Micro QR at
        // two. Every angle, a still image and four live frames of each.
        var symbol = ReferenceSymbols.Get(id);
        using var matrix = symbol.ToMatrix();
        using var rendered = symbol.BarcodeFormat == BbBarcodeFormat.MaxiCode
            ? SyntheticImage.FromMaxiCode(matrix, moduleWidth: pixels)
            : SyntheticImage.FromMatrix(matrix, pixels, quietZoneModules: 2);
        var failures = new List<string>();
        for (var degrees = 5; degrees < 360; degrees += 20)
        {
            using var rotated = ImageTransforms.Rotate(rendered, degrees);
            using var frame = ImageTransforms.Frame(rotated, Math.Max(640, rotated.Width), Math.Max(480, rotated.Height));
            if (Decode(rotated, symbol.BarcodeFormat, still: true)?.Text != symbol.Expected)
            {
                failures.Add($"{degrees} still");
            }

            if (DecodeLive(frame, symbol.BarcodeFormat)?.Text != symbol.Expected)
            {
                failures.Add($"{degrees} live");
            }
        }

        Assert.IsEmpty(failures, $"{id}: {string.Join(", ", failures)}");
    }

    [TestMethod]
    [DataRow("maxicode-mode2")]
    [DataRow("maxicode-numeric")]
    [DataRow("microqr-m3-l")]
    [DataRow("microqr-m4-q")]
    public void ReadsTiltedAwayFromTheCamera(string id)
    {
        // A label tilted back and turned, at four pixels a module, seen in perspective: MaxiCode's grid is the ellipse its
        // rings make, foreshortened away from them as far as its modules' samples are clearest; Micro QR's is the
        // perspective the four sides of its finder pattern give.
        var symbol = ReferenceSymbols.Get(id);
        using var matrix = symbol.ToMatrix();
        using var rendered = symbol.BarcodeFormat == BbBarcodeFormat.MaxiCode
            ? SyntheticImage.FromMaxiCode(matrix, moduleWidth: 4)
            : SyntheticImage.FromMatrix(matrix, 4, quietZoneModules: 2);
        var failures = new List<string>();
        foreach (var tilt in new[] { 20, 35 })
        {
            using var tilted = ImageTransforms.Tilt(rendered, tilt);
            foreach (var degrees in new[] { 0, 30, 125, 250 })
            {
                using var rotated = ImageTransforms.Rotate(tilted, degrees);
                if (Decode(rotated, symbol.BarcodeFormat, still: true)?.Text != symbol.Expected)
                {
                    failures.Add($"tilt {tilt}, {degrees} degrees");
                }
            }
        }

        Assert.IsEmpty(failures, $"{id}: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void NeitherIsMisreadThroughBlurNoiseAndRotation()
    {
        // MaxiCode's Reed-Solomon check over every message, and Micro QR's - M1's two check codewords only detect errors -
        // pushed through blur and noise, turned, live and still: every value read has to be the right one.
        var misreads = new List<string>();
        var (reads, attempts) = (0, 0);
        foreach (var symbol in ReferenceSymbols.All.Where(s => (s.BarcodeFormat & Both) != 0))
        {
            using var rendered = symbol.Render(3);
            foreach (var degrees in new[] { 0, 23, 141 })
            {
                using var rotated = ImageTransforms.Rotate(rendered, degrees);
                foreach (var (blur, noise) in new[] { (0.8, 25), (1.3, 45) })
                {
                    using var blurred = ImageTransforms.Blur(rotated, blur);
                    SyntheticImage.AddNoise(blurred, noise, seed: degrees + noise);
                    using var frame = ImageTransforms.Frame(blurred, Math.Max(640, blurred.Width), Math.Max(480, blurred.Height));
                    foreach (var live in new[] { true, false })
                    {
                        attempts++;
                        if ((live ? DecodeLive(frame, Both) : Decode(blurred, Both, still: true)) is not { } result)
                        {
                            continue;
                        }

                        reads++;
                        if (result.Text != symbol.Expected || result.Format != symbol.BarcodeFormat)
                        {
                            misreads.Add($"{symbol.Id} {degrees} degrees, blur {blur}, noise {noise}, {(live ? "live" : "still")}: {result.Format} {result.Text}");
                        }
                    }
                }
            }
        }

        Assert.IsEmpty(misreads, $"{reads} of {attempts} read; misread: {string.Join("; ", misreads)}");
        Assert.IsGreaterThan(attempts / 2, reads, $"Only {reads} of {attempts} read");
    }

    [TestMethod]
    public void NoOtherSymbologyIsReadAsEither()
    {
        // Every other fixture - a QR symbol's three finder patterns among them, each one Micro QR's finder pattern - and
        // the same through blur and noise: nothing read as MaxiCode or Micro QR, with only those two enabled or with all.
        var wrong = new List<string>();
        foreach (var symbol in ReferenceSymbols.All.Where(s => (s.BarcodeFormat & Both) == 0))
        {
            using var rendered = symbol.Render(symbol.IsLinear ? 2 : 3);
            using var rotated = ImageTransforms.Rotate(rendered, 11);
            using var blurred = ImageTransforms.Blur(rotated, 1.0);
            SyntheticImage.AddNoise(blurred, 30, seed: symbol.Id.Length);
            foreach (var image in new[] { rendered, blurred })
            {
                if (Decode(image, Both, still: true) is { } result)
                {
                    wrong.Add($"{symbol.Id}: {result.Format} {result.Text}");
                }

                if (Decode(image, BbBarcodeFormat.All, still: true) is { Format: BbBarcodeFormat.MaxiCode or BbBarcodeFormat.MicroQrCode } all)
                {
                    wrong.Add($"{symbol.Id} with every symbology: {all.Format} {all.Text}");
                }
            }
        }

        Assert.IsEmpty(wrong, string.Join("; ", wrong));
    }

    [TestMethod]
    public void QrCodeIsStillReadAsQrCodeWithMicroQrEnabled()
    {
        // Micro QR is looked for round the finder patterns only when no QR symbol was read, so a QR symbol is reported as
        // one, and costs nothing more.
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.QrCode).Take(12))
        {
            using var image = symbol.Render(3);
            var result = Decode(image, BbBarcodeFormat.QrCode | BbBarcodeFormat.MicroQrCode, still: true);
            Assert.AreEqual(BbBarcodeFormat.QrCode, result?.Format, symbol.Id);
            Assert.AreEqual(symbol.Expected, result?.Text, symbol.Id);
        }
    }

    [TestMethod]
    public void AnImageOfBullseyeStripesIsGivenUpOnQuickly()
    {
        // Every row crosses a bullseye's runs every 21 pixels, and the column through each centre is light from top to
        // bottom: followed to the image's edge from every row, it cost the square of the height.
        using var stripes = new BbBitMatrix(64, 100_000);
        Fill(stripes, static (x, _) => IsRing(x));

        AssertGivenUpQuickly(stripes);
    }

    [TestMethod]
    public void BullseyeColumnsCrossedByLightRowsAreGivenUpOnQuickly()
    {
        // The rows either side of each light row find bullseyes, their columns confirm them, and the light row through
        // the centre the columns give runs on to the image's edge.
        using var bands = new BbBitMatrix(65_536, 1_024);
        Fill(bands, static (x, y) => (y % 21) switch
        {
            11 => false,
            10 or 12 => IsRing(x),
            _ => IsRing(y),
        });

        AssertGivenUpQuickly(bands);
    }

    [TestMethod]
    public void ALatticeOfBullseyeCrossesIsGivenUpOnQuickly()
    {
        // Each cross is a bullseye along its row and its column, and both its diagonals are light through every other
        // cross to the image's edge.
        using var lattice = new BbBitMatrix(8_192, 8_192);
        Fill(lattice, static (x, y) => IsCentre(y) ? IsRing(x) : IsCentre(x) && IsRing(y));

        AssertGivenUpQuickly(lattice);
    }

    /// <summary>A bullseye's runs at two pixels a ring, every 21 pixels: dark, light, dark, light, dark, the light centre three wide, and out again.</summary>
    private static bool IsRing(int t) => (t % 21) is 0 or 1 or 4 or 5 or 8 or 9 or 13 or 14 or 17 or 18;

    /// <summary>The light centre of <see cref="IsRing"/>.</summary>
    private static bool IsCentre(int t) => (t % 21) is 10 or 11 or 12;

    /// <summary>
    /// Fills an image whose rows repeat every 21, as each above does: the first 21 a bit at a time, every later one
    /// copied a word at a time from the one 21 above it - set one by one, the larger images took seconds.
    /// </summary>
    private static void Fill(BbBitMatrix image, Func<int, int, bool> dark)
    {
        for (var y = 0; y < image.Height; y++)
        {
            if (y >= 21)
            {
                image.GetRowWords(y - 21).CopyTo(image.GetRowWords(y));
                continue;
            }

            for (var x = 0; x < image.Width; x++)
            {
                image[x, y] = dark(x, y);
            }
        }
    }

    /// <summary>
    /// Each image above took from twenty to forty seconds before the walks were bounded and well under a second after;
    /// the bound leaves room for a shared build machine running every target framework's tests side by side.
    /// </summary>
    private static void AssertGivenUpQuickly(BbBitMatrix image)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var result = new BbMaxiCodeReader().Decode(image, new BbMatrixDecodeOptions(TryHarder: true, AllowInverted: false));
        watch.Stop();

        Assert.IsNull(result);
        Assert.IsLessThan(TimeSpan.FromSeconds(10), watch.Elapsed, $"Took {watch.Elapsed}.");
    }

    /// <summary>Up to four live frames of the same image through one decoder, as a camera held still delivers them.</summary>
    private static BbBarcodeResult? DecodeLive(BbLuminanceBuffer frame, BbBarcodeFormat format)
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = format, DuplicateSuppressionWindow = TimeSpan.Zero });
        for (var i = 0; i < 4; i++)
        {
            if (decoder.Decode(frame.View, suppressDuplicates: false) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    private static BbBarcodeResult? Decode(BbLuminanceBuffer image, BbBarcodeFormat format, bool still)
    {
        var options = still ? BbScannerOptions.ForStillImages() : new BbScannerOptions();
        options.Formats = format;
        options.DuplicateSuppressionWindow = TimeSpan.Zero;
        using var decoder = new BbBarcodeDecoder(options);
        return decoder.Decode(image.View, suppressDuplicates: false);
    }
}
