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
                var result = DecodeMiddle(decoderFactory(), image, format);
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
