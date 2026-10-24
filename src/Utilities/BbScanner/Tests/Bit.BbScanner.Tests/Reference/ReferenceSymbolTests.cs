namespace Bit.BbScanner.Tests;

/// <summary>
/// Reads symbols written by an independent encoder (BWIPP, through bwip-js) rather than by the test kit.
/// </summary>
/// <remarks>
/// Every other decoder test reads what the test kit's encoders write, and those were written alongside the
/// decoders: a table entry, a mask or a module placement got wrong identically on both sides would pass every
/// round trip. A production encoder that has never seen this code cannot share such a mistake, so these fixtures
/// are what establish that the decoders read the symbologies as specified rather than as the test kit writes them.
/// </remarks>
[TestClass]
public class ReferenceSymbolTests
{
    public static IEnumerable<object[]> Symbols => ReferenceSymbols.All.Select(s => new object[] { s.Id });

    public static IEnumerable<object[]> MatrixSymbols => ReferenceSymbols.All.Where(s => !s.IsLinear).Select(s => new object[] { s.Id });

    [TestMethod]
    [DynamicData(nameof(Symbols))]
    public void DecodesTheSymbolAsWrittenByAnIndependentEncoder(string id)
    {
        var symbol = ReferenceSymbols.Get(id);
        using var image = symbol.Render(scale: symbol.IsLinear ? 3 : 4);

        var result = Decode(symbol, image, BbScannerOptions.ForStillImages());

        AssertDecoded(symbol, result);
    }

    [TestMethod]
    [DynamicData(nameof(Symbols))]
    public void DecodesTheSymbolWithTheLiveCameraSettings(string id)
    {
        // A live frame is what the component hands the decoder: 640 pixels on its longer edge at most, decoded with
        // the default, latency-first settings. Every fixture fits at two pixels a module or more.
        var symbol = ReferenceSymbols.Get(id);
        using var rendered = symbol.Render(scale: 2);
        using var frame = ImageTransforms.Frame(rendered, Math.Max(640, rendered.Width), Math.Max(480, rendered.Height));

        var result = Decode(symbol, frame, new BbScannerOptions());

        AssertDecoded(symbol, result);
    }

    [TestMethod]
    [DynamicData(nameof(MatrixSymbols))]
    public void DecodesTheSymbolThroughACameraLikeTransform(string id)
    {
        var symbol = ReferenceSymbols.Get(id);
        using var rendered = symbol.Render(scale: 4);
        using var rotated = ImageTransforms.Rotate(rendered, 17);
        using var blurred = ImageTransforms.Blur(rotated, 0.7);
        SyntheticImage.AddNoise(blurred, 12, seed: id.Length);

        var result = Decode(symbol, blurred, BbScannerOptions.ForStillImages());

        if (KnownGaps.TryGetValue(id, out var gap))
        {
            // A known gap is reported rather than hidden, and the list cannot go stale: once the case reads, the
            // entry has to go.
            Assert.IsNull(result, $"{id} now reads through the transform; remove it from {nameof(KnownGaps)}.");
            Assert.Inconclusive(gap);
        }

        AssertDecoded(symbol, result);
    }

    /// <summary>Transformed fixtures the decoders are known not to read yet, keyed by fixture id, with the reason.</summary>
    private static readonly Dictionary<string, string> KnownGaps = [];

    [TestMethod]
    [DataRow("pdf417-text")]
    [DataRow("pdf417-wide")]
    [DataRow("pdf417-security-0")]
    [DataRow("pdf417-driving-licence")]
    public void Pdf417ReadsAtEveryOrientationWithTheLiveSettings(string id)
    {
        // PDF417 rows are three modules tall and up to several hundred wide, so a reader that scans along pixel
        // rows only works within a degree of upright. The locator finds the symbol's orientation first; this
        // sweeps the full circle, at the two and four pixel modules a live frame and a close-up produce.
        var symbol = ReferenceSymbols.Get(id);
        var failures = new List<string>();
        foreach (var scale in new[] { 2, 4 })
        {
            using var rendered = symbol.Render(scale);
            for (var degrees = 0; degrees < 360; degrees += 15)
            {
                using var rotated = ImageTransforms.Rotate(rendered, degrees + 2);
                if (Decode(symbol, rotated, new BbScannerOptions())?.Text != symbol.Expected)
                {
                    failures.Add($"{degrees + 2} degrees at {scale} px a module");
                }
            }
        }

        Assert.IsEmpty(failures, $"{id} was not read at: {string.Join(", ", failures)}");
    }

    [TestMethod]
    [DataRow("aztec-compact-1")]
    [DataRow("aztec-compact-4")]
    [DataRow("aztec-full-4")]
    [DataRow("aztec-full-12")]
    [DataRow("aztec-boarding-pass")]
    public void AztecReadsAtEveryOrientationWithTheLiveSettings(string id)
    {
        // An Aztec bullseye looks the same to a row through its centre at any rotation, but turned near a diagonal it
        // also looks like one to rows a few modules off centre, and a small symbol's square is fitted from a few
        // pixels. The camera settings scan every other row and try fewer candidates than a still image gets, so the
        // full circle is swept with them, at three and four pixels a module.
        var symbol = ReferenceSymbols.Get(id);
        var failures = new List<string>();
        foreach (var scale in new[] { 3, 4 })
        {
            using var rendered = symbol.Render(scale);
            for (var degrees = 0; degrees < 360; degrees += 15)
            {
                using var rotated = ImageTransforms.Rotate(rendered, degrees + 7);
                if (Decode(symbol, rotated, new BbScannerOptions())?.Text != symbol.Expected)
                {
                    failures.Add($"{degrees + 7} degrees at {scale} px a module");
                }
            }
        }

        Assert.IsEmpty(failures, $"{id} was not read at: {string.Join(", ", failures)}");
    }

    [TestMethod]
    [DataRow("aztec-compact-4", 30)]
    [DataRow("aztec-full-4", 30)]
    [DataRow("aztec-full-32", 30)]
    public void AztecReadsTiltedAtEveryRotation(string id, int tilt)
    {
        // Perspective comes from the reference grid on a symbol large enough to have one, and from the outline on one
        // that is not; both are swept.
        var symbol = ReferenceSymbols.Get(id);
        using var rendered = symbol.Render(scale: 4);
        using var tilted = ImageTransforms.Tilt(rendered, tilt);
        var failures = new List<string>();
        for (var degrees = 0; degrees < 360; degrees += 30)
        {
            using var rotated = ImageTransforms.Rotate(tilted, degrees + 7);
            if (Decode(symbol, rotated, BbScannerOptions.ForStillImages())?.Text != symbol.Expected)
            {
                failures.Add($"{degrees + 7}");
            }
        }

        Assert.IsEmpty(failures, $"{id} tilted {tilt} degrees was not read at {string.Join(", ", failures)} degrees.");
    }

    [TestMethod]
    [DataRow("databar-omni-1")]
    [DataRow("databar-omni-5")]
    public void DataBarReadsAtEveryAngleButNearTheDiagonalsWithTheLiveSettings(string id)
    {
        // Omnidirectional DataBar is drawn 33 modules tall, a third of its length, so a scan line can cross it a long
        // way off its own axis: the live angles - upright, sideways and fifteen and thirty degrees off upright, fifteen
        // off sideways - then reach every angle but some ten degrees around the two diagonals they leave furthest
        // apart. Swept round the full circle at the two and three pixel modules a live frame produces.
        var symbol = ReferenceSymbols.Get(id);
        var angles = Enumerable.Range(0, 72).Select(i => 2 + (5 * i))
            .Where(a => (a % 180) is not (>= 45 and <= 60) and not (>= 120 and <= 135));
        var failures = LiveFailures(symbol, modulesTall: 33, [2, 3], angles);
        Assert.IsEmpty(failures, string.Join(", ", failures));
    }

    [TestMethod]
    [DataRow("databar-truncated-0")]
    [DataRow("databar-truncated-1")]
    public void TruncatedDataBarReadsWithinThirtyDegreesOfUprightAndTwentyOfSidewaysWithTheLiveSettings(string id)
    {
        // Truncated DataBar is 13 modules tall, so a line has to run within about eight degrees of its axis to cross it
        // whole, and the live angles cover upright to some 35 degrees either way and sideways to some 20. At two pixels
        // a module the few lines that cross so thin a symbol land on blurred edges often enough to leave gaps between
        // the angles, which the README states rather than this test hiding.
        var symbol = ReferenceSymbols.Get(id);
        var angles = Enumerable.Range(0, 15).Select(i => -33 + (5 * i)).Concat(Enumerable.Range(0, 9).Select(i => 72 + (5 * i)))
            .SelectMany(a => new[] { a, a + 180 });
        var failures = LiveFailures(symbol, modulesTall: 13, [3], angles);
        Assert.IsEmpty(failures, string.Join(", ", failures));
    }

    [TestMethod]
    [DataRow("databar-omni-1", 33)]
    [DataRow("databar-omni-5", 33)]
    [DataRow("databar-truncated-0", 13)]
    [DataRow("databar-truncated-1", 13)]
    public void DataBarReadsAtEveryAngleFromAStillImage(string id, int modulesTall)
    {
        var symbol = ReferenceSymbols.Get(id);
        var failures = new List<string>();
        foreach (var scale in new[] { 2, 3 })
        {
            using var rendered = SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(symbol.Widths), scale, modulesTall * scale);
            for (var degrees = 2; degrees < 360; degrees += 5)
            {
                using var rotated = ImageTransforms.Rotate(rendered, degrees);
                if (Decode(symbol, rotated, BbScannerOptions.ForStillImages())?.Text != symbol.Expected)
                {
                    failures.Add($"{degrees} degrees at {scale} px a module");
                }
            }
        }

        Assert.IsEmpty(failures, $"{id} was not read at: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void DataBarIsNotMisreadThroughBlurNoiseAndRotation()
    {
        // The modulo 79 checksum is DataBar's only check on its data, and the parity of a character's elements is what
        // settles a run measured a module and a half wide. Blur and noise are what make runs measure that way, so the
        // fixtures are pushed through both, turned, with the live and the still image settings, and every value read
        // has to be the right one. A wider sweep run once - 31 104 renders, other seeds, angles and blur, noise up to
        // 60, five pixels a module as well - read 15 026 and misread none; it takes minutes, so it is not kept here.
        var misreads = new List<string>();
        var reads = 0;
        var attempts = 0;
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.DataBar))
        {
            var modulesTall = symbol.Encoder == "databartruncated" ? 13 : 33;
            foreach (var scale in new[] { 2, 3 })
            {
                using var rendered = SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(symbol.Widths), scale, modulesTall * scale);
                foreach (var degrees in new[] { 0, 8, 17, 183 })
                {
                    using var rotated = ImageTransforms.Rotate(rendered, degrees);
                    foreach (var blur in new[] { 0.6, 1.0, 1.4, 1.7 })
                    {
                        foreach (var noise in new[] { 0, 20, 40, 60 })
                        {
                            using var blurred = ImageTransforms.Blur(rotated, blur);
                            if (noise > 0)
                            {
                                SyntheticImage.AddNoise(blurred, noise, seed: degrees + noise);
                            }

                            foreach (var options in new[] { new BbScannerOptions(), BbScannerOptions.ForStillImages() })
                            {
                                attempts++;
                                if (Decode(symbol, blurred, options) is not { } result)
                                {
                                    continue;
                                }

                                reads++;
                                if (result.Text != symbol.Expected)
                                {
                                    misreads.Add($"{symbol.Id} at {scale} px, {degrees} degrees, blur {blur}, noise {noise}: {result.Text}");
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.IsEmpty(misreads, $"{reads} of {attempts} read; misread: {string.Join("; ", misreads)}");
        Assert.IsGreaterThan(attempts / 2, reads, $"Only {reads} of {attempts} read");
    }

    [TestMethod]
    public void NoOtherSymbologyIsReadAsDataBar()
    {
        // DataBar has no start or stop character, only two finder patterns that the bars of another symbology can
        // resemble, and only a modulo 79 checksum behind them; every other fixture is scanned for it with the
        // exhaustive still image settings.
        var misreads = new List<string>();
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat != BbBarcodeFormat.DataBar))
        {
            using var image = symbol.Render(scale: symbol.IsLinear ? 3 : 4);
            var options = BbScannerOptions.ForStillImages();
            options.Formats = BbBarcodeFormat.DataBar;
            using var decoder = new BbBarcodeDecoder(options);
            if (decoder.Decode(image.View, suppressDuplicates: false) is { } result)
            {
                misreads.Add($"{symbol.Id} as {result.Text}");
            }
        }

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void DataBarIsReportedAsDataBarWithEverySymbologyEnabled()
    {
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.DataBar))
        {
            using var image = symbol.Render(scale: 3);
            var options = BbScannerOptions.ForStillImages();
            options.Formats = BbBarcodeFormat.All;
            using var decoder = new BbBarcodeDecoder(options);
            var result = decoder.Decode(image.View, suppressDuplicates: false);

            Assert.AreEqual(BbBarcodeFormat.DataBar, result?.Format, symbol.Id);
            Assert.AreEqual(symbol.Expected, result?.Text, symbol.Id);
        }
    }

    private static List<string> LiveFailures(ReferenceSymbol symbol, int modulesTall, int[] scales, IEnumerable<int> angles)
    {
        var failures = new List<string>();
        foreach (var scale in scales)
        {
            using var rendered = SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(symbol.Widths), scale, modulesTall * scale);
            foreach (var degrees in angles)
            {
                using var rotated = ImageTransforms.Rotate(rendered, degrees);
                using var frame = ImageTransforms.Frame(rotated, Math.Max(640, rotated.Width), Math.Max(480, rotated.Height));
                if (Decode(symbol, frame, new BbScannerOptions())?.Text != symbol.Expected)
                {
                    failures.Add($"{degrees} degrees at {scale} px a module");
                }
            }
        }

        return failures;
    }

    [TestMethod]
    [DataRow("qr-v10-mixed-modes")]
    [DataRow("qr-v25")]
    [DataRow("qr-v40")]
    [DataRow("dm-10x10")]
    [DataRow("dm-text")]
    [DataRow("dm-144x144")]
    [DataRow("dm-rect-16x48")]
    [DataRow("aztec-compact-4")]
    [DataRow("aztec-full-23")]
    [DataRow("aztec-full-32")]
    public void MatrixSymbolsReadAtEveryOrientation(string id)
    {
        // The two failure modes this guards against were both orientation dependent and both invisible to tests
        // that only rotated small symbols: a large QR code whose finder spacing put its dimension estimate two
        // modules between legal sizes was abandoned, and a Data Matrix seen near a diagonal had its L judged on
        // corners a module off. Three pixels a module is the smallest size every orientation is expected at.
        var symbol = ReferenceSymbols.Get(id);
        using var rendered = symbol.Render(scale: 3);
        var failures = new List<string>();
        for (var degrees = 7; degrees < 360; degrees += 15)
        {
            using var rotated = ImageTransforms.Rotate(rendered, degrees);
            if (Decode(symbol, rotated, BbScannerOptions.ForStillImages())?.Text != symbol.Expected)
            {
                failures.Add($"{degrees}");
            }
        }

        Assert.IsEmpty(failures, $"{id} was not read at {string.Join(", ", failures)} degrees.");
    }

    [TestMethod]
    [DynamicData(nameof(MatrixSymbols))]
    public void ReportsTheSymbolCornersClockwiseFromItsOwnTopLeft(string id)
    {
        // Corners are what an application draws an outline with, or crops a scan to, so they have to be the
        // symbol's outer corners - not the QR finder pattern centres three and a half modules in - and in an
        // order that follows the symbol round rather than the frame: clockwise from the symbol's own top-left.
        const int Scale = 4;
        const int QuietZone = 4 * Scale;
        var symbol = ReferenceSymbols.Get(id);
        using var rendered = symbol.Render(Scale);
        var width = symbol.Width * Scale;
        var height = symbol.Rows!.Length * Scale;

        foreach (var degrees in new[] { 0.0, 20, 90, 180, 270 })
        {
            using var rotated = ImageTransforms.Rotate(rendered, degrees);
            var result = Decode(symbol, rotated, BbScannerOptions.ForStillImages());
            Assert.IsNotNull(result, $"{id} at {degrees} degrees was not read.");
            Assert.HasCount(4, result.Corners, $"{id} at {degrees} degrees");

            // Where Rotate put each corner of the symbol: about the centre of the source, into the centre of a
            // canvas large enough to hold the result.
            var radians = degrees * Math.PI / 180;
            var (cos, sin) = (Math.Cos(radians), Math.Sin(radians));
            var (cx, cy) = ((rendered.Width - 1) / 2.0, (rendered.Height - 1) / 2.0);
            var (ox, oy) = ((rotated.Width - 1) / 2.0, (rotated.Height - 1) / 2.0);
            (double X, double Y) Map(double x, double y) => ((cos * (x - cx)) - (sin * (y - cy)) + ox, (sin * (x - cx)) + (cos * (y - cy)) + oy);
            (double X, double Y)[] truth =
            [
                Map(QuietZone, QuietZone),
                Map(QuietZone + width, QuietZone),
                Map(QuietZone + width, QuietZone + height),
                Map(QuietZone, QuietZone + height),
            ];

            for (var i = 0; i < 4; i++)
            {
                var corner = result.Corners[i];
                var distance = Math.Sqrt(Math.Pow(corner.X - truth[i].X, 2) + Math.Pow(corner.Y - truth[i].Y, 2));
                Assert.IsLessThanOrEqualTo(1.5 * Scale, distance,
                    $"{id} at {degrees} degrees: corner {i} is {corner}, expected ({truth[i].X:0.#}, {truth[i].Y:0.#}).");
            }
        }
    }

    [TestMethod]
    public void TheFixturesComeFromTheIndependentEncoder()
    {
        StringAssert.StartsWith(ReferenceSymbols.Generator, "BWIPP");
        Assert.IsGreaterThan(40, ReferenceSymbols.All.Count);
        CollectionAssert.AllItemsAreUnique(ReferenceSymbols.All.Select(s => s.Id).ToList());
    }

    private static BbBarcodeResult? Decode(ReferenceSymbol symbol, BbLuminanceBuffer image, BbScannerOptions options)
    {
        using var decoder = new BbBarcodeDecoder(symbol.Configure(options));
        return decoder.Decode(image.View, suppressDuplicates: false);
    }

    private static void AssertDecoded(ReferenceSymbol symbol, BbBarcodeResult? result)
    {
        var context = $"{symbol.Id} ({symbol.Encoder})";
        Assert.IsNotNull(result, $"{context} was not read.");
        Assert.AreEqual(symbol.BarcodeFormat, result.Format, context);

        if (symbol.Expected is not null)
        {
            Assert.AreEqual(symbol.Expected, result.Text, context);
        }

        if (symbol.ExpectedBytes is { } bytes)
        {
            CollectionAssert.AreEqual(bytes, result.RawBytes.ToArray(), context);
        }

        if (symbol.Gs1)
        {
            Assert.IsTrue(result.Metadata?.IsGs1, $"{context} is a GS1 symbol.");
        }

        if (symbol.Eci is { } eci)
        {
            Assert.AreEqual(eci, result.Metadata?.Eci, context);
        }

        if (symbol.Extension is { } extension)
        {
            Assert.AreEqual(extension, result.Metadata?.Extension, $"{context}: the add-on");
        }
    }
}
