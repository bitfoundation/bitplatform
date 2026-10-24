namespace Bit.BbScanner.Tests;

/// <summary>
/// Guards against wrong readings, which are far worse than missed ones: an application that is
/// handed the wrong number has no way to know.
/// </summary>
[TestClass]
public class LinearMisreadTests
{
    private static BbBitRow Binarize(BbLuminanceBuffer image, int row)
    {
        var bits = new BbBitRow(image.Width);
        Assert.IsTrue(new BbAdaptiveRowBinarizer().TryGetBlackRow(image.View, row, bits));
        return bits;
    }

    private static BbSymbolDecodeResult? DecodeMiddle(IBbRowDecoder decoder, BbLuminanceBuffer image, BbBarcodeFormat formats)
    {
        var middle = image.Height / 2;
        return decoder.DecodeRow(middle, Binarize(image, middle), formats);
    }

    /// <summary>
    /// Renders a symbol with a quiet zone on one side only, the image ending exactly at module
    /// <paramref name="cut"/> on the other.
    /// </summary>
    /// <remarks>
    /// This is what a phone panned across a label actually produces: the symbol runs off the
    /// edge of the frame, so there is no quiet zone to find on that side. Cutting at a fraction
    /// of the image width instead would usually destroy the guard patterns and the symbol would
    /// be rejected for a reason that has nothing to do with truncation.
    /// </remarks>
    private static BbLuminanceBuffer RenderCutAtModule(bool[] modules, int cut, bool fromLeft, int scale = 3)
    {
        const int Quiet = 12;
        const int Height = 40;

        var kept = fromLeft ? modules[cut..] : modules[..cut];
        var width = (kept.Length + Quiet) * scale;
        var buffer = BbLuminanceBuffer.Rent(width, Height);
        buffer.Pixels.Fill(255);

        var offset = fromLeft ? 0 : Quiet * scale;
        for (var i = 0; i < kept.Length; i++)
        {
            if (!kept[i])
            {
                continue;
            }

            var px = offset + (i * scale);
            for (var y = 0; y < Height; y++)
            {
                buffer.Pixels.Slice((y * width) + px, scale).Fill(0);
            }
        }

        return buffer;
    }

    /// <summary>
    /// Cuts a symbol off at every module position, from each side, and collects everything that
    /// still decoded to something other than the true value.
    /// </summary>
    private static List<string> CollectTruncatedReads(
        Func<IBbRowDecoder> decoderFactory, bool[] modules, string expected, BbBarcodeFormat format)
    {
        var misreads = new List<string>();

        foreach (var fromLeft in new[] { false, true })
        {
            for (var cut = 16; cut < modules.Length; cut++)
            {
                using var image = RenderCutAtModule(modules, cut, fromLeft);

                // A cut that leaves too little of the symbol to binarise cannot be misread either.
                var middle = image.Height / 2;
                var bits = new BbBitRow(image.Width);
                if (!new BbAdaptiveRowBinarizer().TryGetBlackRow(image.View, middle, bits))
                {
                    continue;
                }

                var result = decoderFactory().DecodeRow(middle, bits, format);
                if (result is not null && result.Payload.Text != expected)
                {
                    misreads.Add($"{(fromLeft ? "left" : "right")} cut at {cut}: {result.Payload.Text}");
                }
            }
        }

        return misreads;
    }

    [TestMethod]
    public void ATruncatedItfIsNeverReportedAsAShorterNumber()
    {
        // ITF has no check character, so every prefix and suffix of an even number of digits is
        // itself a structurally valid symbol. Before the quiet zone was required to lie inside
        // the frame, this swept up thirteen distinct wrong numbers, among them "1234567890"
        // and "78901231" from the value below.
        const string Digits = "12345678901231";
        var misreads = CollectTruncatedReads(() => new BbItfReader(), LinearEncoders.Itf(Digits), Digits, BbBarcodeFormat.Itf);

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ATruncatedCode39IsNeverReportedAsAShorterValue()
    {
        const string Content = "SHIPMENT-4471";
        var misreads = CollectTruncatedReads(
            () => new BbCode39Reader(), LinearEncoders.Code39(Content), Content, BbBarcodeFormat.Code39);

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ATruncatedCode128IsNeverReportedAsAShorterValue()
    {
        const string Content = "SHIPMENT-4471";
        var misreads = CollectTruncatedReads(
            () => new BbCode128Reader(), LinearEncoders.Code128(Content), Content, BbBarcodeFormat.Code128);

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ATruncatedEan13IsNeverReportedAsAShorterValue()
    {
        const string Digits = "4006381333931";
        var misreads = CollectTruncatedReads(
            () => new BbEanUpcReader(), LinearEncoders.Ean13(Digits), Digits, BbBarcodeFormat.All);

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ATruncatedCodabarIsNeverReportedAsAShorterValue()
    {
        // Codabar has no check character at all; only the start and stop characters at both ends and the quiet
        // zones beyond them stand between a scan line that leaves the symbol early and a shorter wrong value.
        var symbol = ReferenceSymbols.Get("codabar-library");
        var misreads = CollectTruncatedReads(
            () => new BbCodabarReader(), SyntheticImage.ExpandWidths(symbol.Widths), symbol.Expected!, BbBarcodeFormat.Codabar);

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ATruncatedDataBarIsNeverReported()
    {
        // A DataBar symbol is always the same length, so a cut one cannot read as a shorter value; what it must not do
        // is read as a different one, from its finder patterns and whatever print the line runs into.
        var symbol = ReferenceSymbols.Get("databar-omni-1");
        var misreads = CollectTruncatedReads(
            () => new BbDataBarReader(), SyntheticImage.ExpandWidths(symbol.Widths), symbol.Expected!, BbBarcodeFormat.DataBar);

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ASpotOnTheLineThatReadADataBarDoesNotConfirmItself()
    {
        // A white spot nine pixels wide and three tall, inside the right inside character, makes the middle row read a
        // different GTIN that passes every check; the rows three and six pixels away read the right one. Those rows
        // must veto the read, and the lines turned through the middle of the read, which cross the spot again, must not
        // confirm it.
        const string Gtin = "32264919519308";
        using var image = SyntheticImage.FromLinear(LinearEncoders.DataBar(Gtin), scale: 2, height: 66);
        var middle = image.Height / 2;
        for (var y = middle - 1; y <= middle + 1; y++)
        {
            image.Pixels.Slice((y * image.Width) + (image.Width / 2) + 10, 9).Fill(255);
        }

        var expected = "01" + Gtin[..13] + LinearEncoders.EanCheckDigit(Gtin[..13]);
        var spotted = DecodeMiddle(new BbDataBarReader(), image, BbBarcodeFormat.DataBar)?.Payload.Text;
        Assert.IsNotNull(spotted, "The spot no longer makes the middle row read anything, so this test no longer tests.");
        Assert.AreNotEqual(expected, spotted, "The spot no longer makes the middle row misread, so this test no longer tests.");

        foreach (var options in new[] { new BbScannerOptions(), BbScannerOptions.ForStillImages() })
        {
            options.Formats = BbBarcodeFormat.DataBar;
            options.DuplicateSuppressionWindow = TimeSpan.Zero;
            using var decoder = new BbBarcodeDecoder(options);
            var text = decoder.Decode(image.View, suppressDuplicates: false)?.Text;
            Assert.IsTrue(text is null || text == expected, $"Read {text} for {expected}.");
        }
    }

    [TestMethod]
    [DataRow("51378631303345", 4, -2)]
    [DataRow("76087885716663", 2, -1)]
    [DataRow("19829147344277", 2, 1)]
    public void DataBarPrintedWithWideOrNarrowBarsIsNotMisread(string gtin, int scale, int barGain)
    {
        // Bars half a module wider or narrower than their modules - ink spread, a thermal printer's dot gain, an
        // underexposed photograph - put every element's rounding in doubt by the same amount on every line, so a
        // second line cannot catch a wrong reading of it: the bias has to be measured and taken out.
        var expected = "01" + gtin[..13] + LinearEncoders.EanCheckDigit(gtin[..13]);
        var wrong = new List<string>();
        using var rendered = SyntheticImage.FromLinearWithBarGain(LinearEncoders.DataBar(gtin), scale, barGain, height: 33 * scale);
        foreach (var blur in new[] { 0.0, 0.7 })
        {
            foreach (var degrees in new[] { 0, 4 })
            {
                using var rotated = ImageTransforms.Rotate(rendered, degrees);
                using var blurred = blur == 0 ? null : ImageTransforms.Blur(rotated, blur);
                foreach (var options in new[] { new BbScannerOptions(), BbScannerOptions.ForStillImages() })
                {
                    options.Formats = BbBarcodeFormat.DataBar;
                    options.DuplicateSuppressionWindow = TimeSpan.Zero;
                    using var decoder = new BbBarcodeDecoder(options);
                    if (decoder.Decode((blurred ?? rotated).View, suppressDuplicates: false)?.Text is { } text && text != expected)
                    {
                        wrong.Add($"blur {blur}, {degrees} degrees: {text}");
                    }
                }
            }
        }

        Assert.IsEmpty(wrong, $"{expected} misread as {string.Join("; ", wrong)}");
    }

    [TestMethod]
    public void DataBarWithSpotsOrBiasedBarsIsNeverMisread()
    {
        // The two faults a second scan line is weakest against, swept over random GTINs: a spot that only the reading
        // line runs through, at every position along the symbol, and bars a pixel or two wider or narrower than their
        // modules, sharp and blurred, square and turned. A wider run of the same sweeps - 60 GTINs with a spot every
        // two pixels, 40 with every bias - read 10 920 of 10 920 and 3 608 of 4 800, and misread none.
        var random = new Random(12);
        var wrong = new List<string>();
        var reads = 0;
        var attempts = 0;
        for (var n = 0; n < 6; n++)
        {
            var digits = string.Concat(Enumerable.Range(0, 13).Select(_ => (char)('0' + random.Next(10))));
            var expected = "01" + digits + LinearEncoders.EanCheckDigit(digits);
            var modules = LinearEncoders.DataBar(digits);

            using var clean = SyntheticImage.FromLinear(modules, scale: 2, height: 66);
            var middle = clean.Height / 2;
            for (var x = 24; x + 9 < clean.Width - 24; x += 10)
            {
                using var spotted = ImageTransforms.Frame(clean, clean.Width, clean.Height);
                for (var y = middle - 1; y <= middle + 1; y++)
                {
                    spotted.Pixels.Slice((y * spotted.Width) + x, 9).Fill(255);
                }

                Check(spotted, $"spot at {x}");
            }

            foreach (var (scale, gain) in new[] { (2, -1), (2, 1), (3, -2), (3, 2), (4, -2), (4, 2) })
            {
                using var biased = SyntheticImage.FromLinearWithBarGain(modules, scale, gain, height: 33 * scale);
                foreach (var degrees in new[] { 0, 4 })
                {
                    using var rotated = ImageTransforms.Rotate(biased, degrees);
                    using var blurred = ImageTransforms.Blur(rotated, 0.7);
                    Check(rotated, $"{scale} px, bars {gain} px, {degrees} degrees");
                    Check(blurred, $"{scale} px, bars {gain} px, {degrees} degrees, blurred");
                }
            }

            void Check(BbLuminanceBuffer image, string what)
            {
                foreach (var options in new[] { new BbScannerOptions(), BbScannerOptions.ForStillImages() })
                {
                    options.Formats = BbBarcodeFormat.DataBar;
                    options.DuplicateSuppressionWindow = TimeSpan.Zero;
                    using var decoder = new BbBarcodeDecoder(options);
                    attempts++;
                    if (decoder.Decode(image.View, suppressDuplicates: false)?.Text is not { } text)
                    {
                        continue;
                    }

                    reads++;
                    if (text != expected)
                    {
                        wrong.Add($"{expected}, {what}: {text}");
                    }
                }
            }
        }

        Assert.IsEmpty(wrong, string.Join("; ", wrong));
        Assert.IsGreaterThan(attempts / 2, reads, $"Only {reads} of {attempts} read.");
    }

    [TestMethod]
    public void ADataBarEdgeMisplacedByAModuleIsNeverMisread()
    {
        // Moving one edge a module widens one element and narrows its neighbour, which is a different but perfectly
        // sharp character. Its odd and even elements then add up to the wrong parity, and a run measured a whole number
        // of modules wide is not rounded the other way to put that right: that would leave the modulo 79 checksum
        // alone between the damage and a wrong GTIN. Every edge of every fixture is moved both ways.
        var misreads = new List<string>();
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.DataBar))
        {
            var widths = symbol.Widths!;
            for (var edge = 1; edge + 1 < widths.Length; edge++)
            {
                foreach (var shift in new[] { -1, 1 })
                {
                    var damaged = (int[])widths.Clone();
                    damaged[edge] += shift;
                    damaged[edge + 1] -= shift;
                    if (damaged[edge] < 1 || damaged[edge + 1] < 1)
                    {
                        continue;
                    }

                    using var image = SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(damaged), scale: 3, height: 30);
                    var result = DecodeMiddle(new BbDataBarReader(), image, BbBarcodeFormat.DataBar);
                    if (result is not null && result.Payload.Text != symbol.Expected)
                    {
                        misreads.Add($"{symbol.Id}, edge {edge} moved {shift}: {result.Payload.Text}");
                    }
                }
            }
        }

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ATruncatedCode93IsNeverReportedAsAShorterValue()
    {
        var symbol = ReferenceSymbols.Get("code93");
        var misreads = CollectTruncatedReads(
            () => new BbCode93Reader(), SyntheticImage.ExpandWidths(symbol.Widths), symbol.Expected!, BbBarcodeFormat.Code93);

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void ACutThroughAnAddOnNeverReportsAWrongAddOn()
    {
        // The main symbol reads either way; the add-on beside it must come out whole or not at all.
        var symbol = ReferenceSymbols.Get("ean13-addon5");
        var modules = SyntheticImage.ExpandWidths(symbol.Widths);
        var wrong = new List<string>();
        for (var cut = 96; cut < modules.Length; cut++)
        {
            using var image = RenderCutAtModule(modules, cut, fromLeft: false);
            var result = DecodeMiddle(new BbEanUpcReader(), image, BbBarcodeFormat.Ean13);
            if (result?.Payload.Extension is { } extension && extension != symbol.Extension)
            {
                wrong.Add($"cut at {cut}: {extension}");
            }
        }

        Assert.IsEmpty(wrong);

        using var whole = SyntheticImage.FromLinear(modules, scale: 3, height: 40);
        Assert.AreEqual(symbol.Extension, DecodeMiddle(new BbEanUpcReader(), whole, BbBarcodeFormat.Ean13)?.Payload.Extension);
    }

    [TestMethod]
    public void RequiringTheAddOnHoldsBackASymbolWithoutOne()
    {
        var symbol = ReferenceSymbols.Get("ean13");
        using var image = SyntheticImage.FromLinear(SyntheticImage.ExpandWidths(symbol.Widths), scale: 3, height: 40);

        Assert.IsNotNull(DecodeMiddle(new BbEanUpcReader(), image, BbBarcodeFormat.Ean13));
        Assert.IsNull(DecodeMiddle(new BbEanUpcReader(requireExtension: true), image, BbBarcodeFormat.Ean13));
    }

    [TestMethod]
    public void IntactSymbolsStillDecodeAfterTheTruncationGuards()
    {
        using var itf = SyntheticImage.FromLinear(LinearEncoders.Itf("12345678901231"), scale: 3, height: 40);
        Assert.AreEqual("12345678901231", DecodeMiddle(new BbItfReader(), itf, BbBarcodeFormat.Itf)?.Payload.Text);

        using var code39 = SyntheticImage.FromLinear(LinearEncoders.Code39("SHIPMENT-4471"), scale: 3, height: 40);
        Assert.AreEqual("SHIPMENT-4471", DecodeMiddle(new BbCode39Reader(), code39, BbBarcodeFormat.Code39)?.Payload.Text);

        using var code128 = SyntheticImage.FromLinear(LinearEncoders.Code128("SHIPMENT-4471"), scale: 3, height: 40);
        Assert.AreEqual("SHIPMENT-4471", DecodeMiddle(new BbCode128Reader(), code128, BbBarcodeFormat.Code128)?.Payload.Text);

        using var ean = SyntheticImage.FromLinear(LinearEncoders.Ean13("4006381333931"), scale: 3, height: 40);
        Assert.AreEqual("4006381333931", DecodeMiddle(new BbEanUpcReader(), ean, BbBarcodeFormat.All)?.Payload.Text);
    }

    [TestMethod]
    public void ItfRejectsALengthOutsideTheAcceptedSet()
    {
        using var image = SyntheticImage.FromLinear(LinearEncoders.Itf("1234567890"), scale: 3, height: 40);

        Assert.IsNull(DecodeMiddle(new BbItfReader([14]), image, BbBarcodeFormat.Itf));
        Assert.IsNotNull(DecodeMiddle(new BbItfReader([10]), image, BbBarcodeFormat.Itf));
    }

    [TestMethod]
    public void DamagedEan13IsNotReportedAsUpcE()
    {
        // The first-digit parity table of EAN-13 and the number-system-1 table of UPC-E are the
        // same, so a right half that fails to read used to be reported as a UPC-E whose expanded
        // checksum happened to pass.
        var misreads = 0;
        var misreadDetails = new List<string>();
        var random = new Random(20260908);

        for (var attempt = 0; attempt < 400; attempt++)
        {
            var digits = string.Create(13, random, static (span, rng) =>
            {
                for (var i = 0; i < 12; i++)
                {
                    span[i] = (char)('0' + rng.Next(10));
                }

                span[12] = LinearEncoders.EanCheckDigit(new string(span[..12]));
            });

            var modules = LinearEncoders.Ean13(digits);

            // Blank out a run of modules in the right half, as a scratch or a fold would. The
            // run starts anywhere from the first bar after the middle guard, which is where the
            // left half becomes a complete UPC-E candidate, to well into the right half.
            var start = 50 + random.Next(30);
            for (var i = start; i < Math.Min(modules.Length, start + 14); i++)
            {
                modules[i] = false;
            }

            using var image = SyntheticImage.FromLinear(modules, scale: 3, height: 40);
            var result = DecodeMiddle(new BbEanUpcReader(), image, BbBarcodeFormat.All);

            if (result is not null && result.Payload.Text != digits)
            {
                misreads++;
                misreadDetails.Add($"start={start} digits={digits} got={result.Format}:{result.Payload.Text}");
            }
        }

        // The left half of an EAN-13 followed by its middle guard and the first bar of the
        // seventh digit is, element for element, a complete UPC-E; only what follows tells them
        // apart. The reader demands the seven module quiet zone the UPC-E specification
        // requires, which leaves a residual only when the damage begins exactly there, and the
        // pipeline then re-reads on parallel lines (see the next test). Anything above a
        // fraction of a percent at row level is a regression.
        Assert.IsTrue(misreads * 200 <= 400, string.Join("; ", misreadDetails));
    }

    [TestMethod]
    public void AScratchAcrossAnEan13IsNotConfirmedAsAUpcE()
    {
        // The one row-level misread above: blanked from the first bar of the seventh digit.
        const string Digits = "4514621479405";
        var modules = LinearEncoders.Ean13(Digits);
        using var image = SyntheticImage.FromLinear(modules, scale: 3, height: 40);

        // A scratch a few pixels tall across the middle of the symbol, where the pipeline's
        // first scan line falls. The lines it confirms on, a few pixels either side, are clean.
        var scratchStart = 52 * 3 + (12 * 3);
        for (var y = 19; y <= 21; y++)
        {
            image.Pixels.Slice((y * image.Width) + scratchStart, 14 * 3).Fill(255);
        }

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions
        {
            Formats = BbBarcodeFormat.Ean13 | BbBarcodeFormat.UpcE,
            DuplicateSuppressionWindow = TimeSpan.Zero,
        });

        var result = decoder.Decode(image.View, suppressDuplicates: false);
        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Ean13, result.Format);
        Assert.AreEqual(Digits, result.Text);
    }

    [TestMethod]
    public void Code39RejectsAnIllegalWideToNarrowRatio()
    {
        // The specification requires between 2:1 and 3:1. A printer that produced 1.4:1 is out
        // of tolerance and its output must not be read as if it were valid.
        var modules = LinearEncoders.Code39("ABC", wideRatio: 1);
        using var image = SyntheticImage.FromLinear(modules, scale: 5, height: 40);

        Assert.IsNull(DecodeMiddle(new BbCode39Reader(), image, BbBarcodeFormat.Code39));
    }

    [TestMethod]
    public void Code39ValidatesTheCheckCharacterWhenAsked()
    {
        // "ABC" with its modulo 43 check character appended is valid; without it, it is not.
        const string Content = "ABC";
        var total = 0;
        foreach (var c in Content)
        {
            total += "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%".IndexOf(c, StringComparison.Ordinal);
        }

        var check = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%"[total % 43];

        using var valid = SyntheticImage.FromLinear(LinearEncoders.Code39(Content + check), scale: 3, height: 40);
        using var invalid = SyntheticImage.FromLinear(LinearEncoders.Code39(Content + "Z"), scale: 3, height: 40);

        var reader = new BbCode39Reader(validateCheckCharacter: true);
        var accepted = DecodeMiddle(reader, valid, BbBarcodeFormat.Code39);
        Assert.IsNotNull(accepted);
        Assert.AreEqual(Content, accepted.Payload.Text);

        Assert.IsNull(DecodeMiddle(reader, invalid, BbBarcodeFormat.Code39));
    }

    [TestMethod]
    public void Code39FullAsciiRoundTripsWhenEnabled()
    {
        // Full ASCII encodes a lower case letter as a "+" escape followed by the upper case one.
        using var image = SyntheticImage.FromLinear(LinearEncoders.Code39("+A+B+C"), scale: 3, height: 40);

        var result = DecodeMiddle(new BbCode39Reader(extendedMode: true), image, BbBarcodeFormat.Code39);

        Assert.IsNotNull(result);
        Assert.AreEqual("abc", result.Payload.Text);
    }

    [TestMethod]
    public void RandomRowsNeverProduceAResult()
    {
        // Every linear decoder runs on every scan line of every camera frame, most of which are
        // noise. One accepted row in a million is a wrong value handed to an application.
        var random = new Random(4242);
        var decoders = new IBbRowDecoder[]
        {
            new BbCode128Reader(), new BbCode39Reader(), new BbEanUpcReader(), new BbItfReader(),
            new BbCode93Reader(), new BbCodabarReader(), new BbCodabarReader(keepStartStop: true), new BbEanUpcReader(requireExtension: true),
            new BbDataBarReader(),
        };

        var accepted = new List<string>();
        var row = new BbBitRow(640);

        for (var attempt = 0; attempt < 20_000; attempt++)
        {
            row.Reset(640);
            var run = 0;
            var value = false;
            for (var i = 0; i < 640; i++)
            {
                if (run == 0)
                {
                    run = 1 + random.Next(8);
                    value = !value;
                }

                row[i] = value;
                run--;
            }

            foreach (var decoder in decoders)
            {
                var result = decoder.DecodeRow(0, row, BbBarcodeFormat.All);
                if (result is not null)
                {
                    accepted.Add($"{result.Format}: {result.Payload.Text}");
                }
            }
        }

        Assert.IsEmpty(accepted);
    }

    [TestMethod]
    public void EveryLinearDecoderSurvivesAdversarialRowsWithoutThrowing()
    {
        var random = new Random(7);
        var decoders = new IBbRowDecoder[]
        {
            new BbCode128Reader(), new BbCode39Reader(), new BbEanUpcReader(), new BbItfReader(),
            new BbCode93Reader(), new BbCodabarReader(), new BbCodabarReader(keepStartStop: true), new BbEanUpcReader(requireExtension: true),
            new BbDataBarReader(),
        };

        foreach (var size in new[] { 1, 2, 3, 31, 32, 33, 64, 257 })
        {
            var row = new BbBitRow(size);
            for (var attempt = 0; attempt < 200; attempt++)
            {
                row.Reset(size);
                for (var i = 0; i < size; i++)
                {
                    row[i] = random.Next(2) == 0;
                }

                foreach (var decoder in decoders)
                {
                    // The contract is that a decoder reports failure by returning null.
                    decoder.DecodeRow(0, row, BbBarcodeFormat.All);
                    row.Reverse();
                    decoder.DecodeRow(0, row, BbBarcodeFormat.All);
                    row.Reverse();
                }
            }
        }
    }
}
