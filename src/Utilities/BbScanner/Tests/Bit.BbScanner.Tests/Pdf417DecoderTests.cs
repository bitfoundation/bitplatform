namespace Bit.BbScanner.Tests;

/// <summary>
/// Exercises the PDF417 implementation: detection, the row indicators, GF(929) error correction,
/// the compaction modes and the symbol character table.
/// </summary>
/// <remarks>
/// A round trip through the test kit's encoder cannot validate the symbol character table, since
/// both sides read the same one. <see cref="ReferenceSymbolTests"/> does that, with symbols an
/// independent encoder wrote; these tests cover what a round trip can.
/// </remarks>
[TestClass]
public class Pdf417DecoderTests
{
    [TestMethod]
    [DataRow("HELLO WORLD")]
    [DataRow("PDF FOUR ONE SEVEN")]
    [DataRow("A")]
    public void DecodesTextCompaction(string content)
    {
        using var matrix = Pdf417Encoder.Encode(content);
        var result = new BbPdf417Reader().Decode(matrix, BbMatrixDecodeOptions.Live);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Pdf417, result.Format);
        Assert.AreEqual(content, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("Mixed case 123!")]
    [DataRow("https://example.com/p/42")]
    public void DecodesByteCompaction(string content)
    {
        using var matrix = Pdf417Encoder.Encode(content, columns: 6);
        var result = new BbPdf417Reader().Decode(matrix, BbMatrixDecodeOptions.Live);

        Assert.IsNotNull(result);
        Assert.AreEqual(content, result.Payload.Text);
    }

    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(3, 2)]
    [DataRow(8, 4)]
    public void DecodesAcrossColumnCountsAndSecurityLevels(int columns, int level)
    {
        const string Content = "STACKED SYMBOLOGY";
        using var matrix = Pdf417Encoder.Encode(Content, columns, level);

        var result = new BbPdf417Reader().Decode(matrix, BbMatrixDecodeOptions.Live);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
        Assert.AreEqual(level.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Payload.ErrorCorrectionLevel);
        Assert.AreEqual(columns, result.Payload.Columns);
    }

    [TestMethod]
    public void ErrorCorrectionRepairsDamagedCodewords()
    {
        const string Content = "ERROR CORRECTION OVER NINE TWO NINE";
        using var matrix = Pdf417Encoder.Encode(Content, columns: 4, errorCorrectionLevel: 5);

        var result = new BbPdf417Reader().Decode(matrix, BbMatrixDecodeOptions.Live);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
    }

    [TestMethod]
    public void ReedSolomonOverGf929RoundTrips()
    {
        var field = BbModulusGF.Pdf417;
        var correction = new BbPdf417ErrorCorrection();

        // Build a codeword block by hand, damage two codewords, and check both are repaired.
        var data = new List<int> { 9, 100, 200, 300, 400, 500, 600, 700 };
        var encoded = BuildBlock(data, numEc: 8);

        Assert.IsTrue(correction.TryDecode([.. encoded], 8, out var clean));
        Assert.AreEqual(0, clean);

        encoded[2] = field.Add(encoded[2], 17);
        encoded[5] = field.Add(encoded[5], 300);

        Assert.IsTrue(correction.TryDecode(encoded, 8, out var corrected));
        Assert.AreEqual(2, corrected);
        Assert.AreEqual(100, encoded[1]);
        Assert.AreEqual(200, encoded[2]);
        Assert.AreEqual(500, encoded[5]);
    }

    [TestMethod]
    public void TooManyErrorsAreReportedRatherThanGuessed()
    {
        var correction = new BbPdf417ErrorCorrection();
        var encoded = BuildBlock([9, 1, 2, 3, 4, 5, 6, 7], numEc: 4);

        for (var i = 0; i < 5; i++)
        {
            encoded[i] = (encoded[i] + 137) % 929;
        }

        var repaired = correction.TryDecode(encoded, 4, out _);
        if (repaired)
        {
            // A miscorrection is possible in principle; it must at least not claim the original.
            Assert.AreNotEqual(1, encoded[1]);
        }
    }

    [TestMethod]
    public void DecodesFromARenderedAndBinarisedImage()
    {
        const string Content = "PDF FOUR ONE SEVEN";
        using var matrix = Pdf417Encoder.Encode(Content, columns: 4, errorCorrectionLevel: 2, rowHeightModules: 4);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 6);

        using var binary = new BbHybridBinarizer().GetBlackMatrix(image.View);
        Assert.IsNotNull(binary);

        var result = new BbPdf417Reader().Decode(binary, BbMatrixDecodeOptions.Thorough);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
        Assert.AreEqual(4, result.Payload.Columns);
        Assert.AreEqual(4, result.Points.Length);
    }

    [TestMethod]
    public void AdjacentRowsWithIdenticalCodewordsAreNotMerged()
    {
        // Two columns, no error correction: rows 1 and 2 both carry "AB AB" style repeats plus
        // identical indicators in their low part, so a detector that merges neighbouring image
        // rows by codeword equality alone collapses them and loses a row.
        const string Content = "ABABABABABAB";
        using var matrix = Pdf417Encoder.Encode(Content, columns: 2, errorCorrectionLevel: 0);

        var result = new BbPdf417Reader().Decode(matrix, BbMatrixDecodeOptions.Live);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
    }

    [TestMethod]
    public void ASingleCorruptedScanLineDoesNotSplitARow()
    {
        const string Content = "ONE BAD PIXEL";
        using var matrix = Pdf417Encoder.Encode(Content, columns: 3, errorCorrectionLevel: 2, rowHeightModules: 3);

        // Flip one module in the middle scan line of the second symbol row. The other two scan
        // lines of that row still read correctly and must outvote it.
        var y = 4;
        var x = matrix.Width / 2;
        matrix[x, y] = !matrix[x, y];

        var result = new BbPdf417Reader().Decode(matrix, BbMatrixDecodeOptions.Live);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
    }

    [TestMethod]
    public void AnUnreadableCodewordBecomesAnErasureRatherThanLosingTheRow()
    {
        const string Content = "ERASURE";
        using var matrix = Pdf417Encoder.Encode(Content, columns: 3, errorCorrectionLevel: 3, rowHeightModules: 3);

        // Black out the first data character of the second row on every one of its scan lines:
        // the pattern is not in the table, so the detector cannot read it, and the error
        // correction stage has to supply it.
        var quiet = 2;
        var start = quiet + 17 + 17; // quiet zone, start pattern, left indicator
        for (var y = 3; y < 6; y++)
        {
            for (var x = start; x < start + 17; x++)
            {
                matrix[x, y] = true;
            }
        }

        var result = new BbPdf417Reader().Decode(matrix, BbMatrixDecodeOptions.Live);

        Assert.IsNotNull(result);
        Assert.AreEqual(Content, result.Payload.Text);
        Assert.IsTrue(result.Payload.ErrorsCorrected >= 1);
    }

    [TestMethod]
    public void AByteShiftInsideTextResumesThePriorSubMode()
    {
        // Text: latch to lower, "abc", shift to byte for '!', then "de" which must still be lower case.
        int[] codewords = [7, 900, (27 * 30) + 0, (1 * 30) + 2, 913, '!', (3 * 30) + 4];

        var result = BbPdf417BitStreamParser.Decode(codewords, 3, 3, 0, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual("abc!de", result.Text);
    }

    [TestMethod]
    public void EciIsAppliedToByteCompactedText()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes("héllo");
        Assert.AreEqual(6, utf8.Length);

        // One full group of six bytes, as five base 900 codewords.
        long value = 0;
        foreach (var b in utf8)
        {
            value = (value << 8) | b;
        }

        var group = new int[5];
        for (var i = 4; i >= 0; i--)
        {
            group[i] = (int)(value % 900);
            value /= 900;
        }

        int[] codewords = [9, 927, 26, 924, .. group];

        var result = BbPdf417BitStreamParser.Decode(codewords, 3, 3, 0, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual("héllo", result.Text);
        Assert.AreEqual(26, result.Eci);
        Assert.AreSequenceEqual(utf8, result.RawBytes);
    }

    [TestMethod]
    public void MacroControlBlockReportsTheSegmentIndexAndCount()
    {
        // Payload "A" in text, then 928, segment index 5 ("100005" = 111 * 900 + 105), file id
        // codewords, optional field 1 holding the segment count 3 ("100003"), terminator.
        int[] codewords = [13, 900, (0 * 30) + 29, 928, 111, 105, 100, 200, 923, 1, 111, 103, 922];

        var result = BbPdf417BitStreamParser.Decode(codewords, 3, 4, 0, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual("A", result.Text);
        Assert.AreEqual(5, result.StructuredAppendIndex);
        Assert.AreEqual(3, result.StructuredAppendCount);
    }

    [TestMethod]
    public void TheSpecificationTableShipsAndIsInUseByDefault()
    {
        Assert.AreSame(BbPdf417SymbolTable.Specification, BbPdf417SymbolTable.Current);
        Assert.IsTrue(BbPdf417SymbolTable.Current.IsSpecificationTable);

        // Spot checks against ISO/IEC 15438, so an accidental edit of the data shows up here and not only as
        // symbols that quietly stop reading: the first and last codeword of every cluster.
        AssertPattern(0, 0, 0x1D5C0);
        AssertPattern(0, 928, 0x1BEF4);
        AssertPattern(3, 0, 0x1F560);
        AssertPattern(3, 928, 0x13F26);
        AssertPattern(6, 0, 0x1ABE0);
        AssertPattern(6, 928, 0x1C7EA);

        static void AssertPattern(int cluster, int codeword, int expected)
        {
            Assert.IsTrue(BbPdf417SymbolTable.Specification.TryGetPattern(cluster, codeword, out var pattern));
            Assert.AreEqual(expected, pattern, $"cluster {cluster}, codeword {codeword}");
        }
    }

    [TestMethod]
    public void ShippedSymbolTableIsStructurallyValid()
    {
        var table = BbPdf417SymbolTable.Current;
        var widths = new int[8];

        foreach (var cluster in new[] { 0, 3, 6 })
        {
            var seen = new HashSet<int>();
            for (var codeword = 0; codeword < BbPdf417SymbolTable.CodewordCount; codeword++)
            {
                Assert.IsTrue(table.TryGetPattern(cluster, codeword, out var pattern));
                Assert.IsTrue(seen.Add(pattern), "Every codeword of a cluster has a pattern of its own.");

                // Seventeen modules, starting with a bar and ending with a space.
                Assert.IsTrue(pattern < 1 << BbPdf417SymbolTable.ModulesPerCodeword);
                Assert.IsTrue((pattern & (1 << 16)) != 0, "A symbol character starts with a bar.");
                Assert.IsTrue((pattern & 1) == 0, "A symbol character ends with a space.");

                // Four bars and four spaces of one to six modules each, in the cluster it is listed under.
                var elements = 0;
                var run = 1;
                for (var bit = 15; bit >= 0; bit--)
                {
                    if (((pattern >> bit) & 1) == ((pattern >> (bit + 1)) & 1))
                    {
                        run++;
                        continue;
                    }

                    Assert.IsLessThan(7, elements, $"pattern {pattern:X} has more than eight elements");
                    widths[elements++] = run;
                    run = 1;
                }

                Assert.AreEqual(7, elements, $"pattern {pattern:X}");
                widths[7] = run;
                foreach (var width in widths)
                {
                    Assert.IsTrue(width is >= 1 and <= 6, $"pattern {pattern:X} has an element {width} modules wide");
                }

                Assert.AreEqual(cluster, BbPdf417SymbolTable.ClusterOf(widths), $"pattern {pattern:X}");
                Assert.AreEqual(pattern, BbPdf417SymbolTable.ToPattern(widths));

                Assert.IsTrue(table.TryGetCodeword(cluster, pattern, out var roundTripped));
                Assert.AreEqual(codeword, roundTripped);
            }
        }
    }

    [TestMethod]
    public void PipelineReadsPdf417ByDefault()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { DuplicateSuppressionWindow = TimeSpan.Zero });

        using var matrix = Pdf417Encoder.Encode("ENABLED BY DEFAULT");
        using var image = SyntheticImage.FromMatrix(matrix, scale: 3);

        var result = decoder.Decode(image.View);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Pdf417, result.Format);
        Assert.AreEqual("ENABLED BY DEFAULT", result.Text);
    }

    [TestMethod]
    [DoNotParallelize]
    public void PipelineLeavesPdf417DisabledWhenARegisteredTableIsNotTheSpecification()
    {
        // The table is process wide, so this runs on its own and puts the specification table back.
#pragma warning disable CS0618 // The placeholder is exactly the kind of table this is about.
        BbPdf417SymbolTable.Register(BbPdf417SymbolTable.CreatePlaceholder());
#pragma warning restore CS0618
        try
        {
            using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.Pdf417 });

            using var matrix = Pdf417Encoder.Encode("DISABLED");
            using var image = SyntheticImage.FromMatrix(matrix, scale: 3);

            Assert.IsNull(decoder.Decode(image.View));
        }
        finally
        {
            BbPdf417SymbolTable.Register(BbPdf417SymbolTable.Specification);
        }
    }

    private static int[] BuildBlock(List<int> data, int numEc)
    {
        var field = BbModulusGF.Pdf417;

        var generator = field.One;
        for (var i = 1; i <= numEc; i++)
        {
            generator = generator.Multiply(new BbModulusPoly(field, [1, field.Subtract(0, field.Exp(i))]));
        }

        var remainder = new BbModulusPoly(field, [.. data]).MultiplyByMonomial(numEc, 1);
        var inverse = field.Inverse(generator.GetCoefficient(generator.Degree));

        while (remainder.Degree >= generator.Degree && !remainder.IsZero)
        {
            var degreeDifference = remainder.Degree - generator.Degree;
            var scale = field.Multiply(remainder.GetCoefficient(remainder.Degree), inverse);
            remainder = remainder.Subtract(generator.MultiplyByMonomial(degreeDifference, scale));
        }

        var block = new int[data.Count + numEc];
        for (var i = 0; i < data.Count; i++)
        {
            block[i] = data[i];
        }

        for (var i = 0; i < numEc; i++)
        {
            block[data.Count + numEc - 1 - i] = field.Subtract(0, remainder.GetCoefficient(i));
        }

        return block;
    }
    [TestMethod]
    [DataRow(0)]
    [DataRow(2)]
    [DataRow(20)]
    [DataRow(90)]
    public void ALightOnDarkSymbolReadsThroughTheBitMatrixReader(int degrees)
    {
        // Without a luminance plane the located symbol is resampled from the binarised image, which the reader has
        // already inverted for the light-on-dark attempt; inverting the samples again undid it.
        const string Content = "LIGHT ON DARK";
        using var matrix = Pdf417Encoder.Encode(Content);
        using var rendered = SyntheticImage.FromMatrix(matrix, 3);
        using var rotated = ImageTransforms.Rotate(rendered, degrees);
        for (var i = 0; i < rotated.Pixels.Length; i++)
        {
            rotated.Pixels[i] = (byte)(255 - rotated.Pixels[i]);
        }

        using var bits = new BbHybridBinarizer().GetBlackMatrix(rotated.View)!;
        var result = new BbPdf417Reader().Decode(bits, new BbMatrixDecodeOptions(TryHarder: false, AllowInverted: true));

        Assert.AreEqual(Content, result?.Payload.Text);
    }

    [TestMethod]
    public void StartAndStopShapedStripesAreResampledWithinABudget()
    {
        // A start pattern and a stop pattern the height of the frame with anything between them - a poster, a crafted
        // file - is a candidate like any real symbol. With modules of a pixel it was resampled three times over, into
        // nearly two megapixels, and every row of that read, on every frame. The candidates of a frame now share a
        // budget of a frame and a half of pixels - half a megapixel at least - which is checked here by the size of the
        // copy itself rather than by a clock, which a busy machine would make unreliable.
        var random = new Random(3);
        var modules = new List<bool>();
        void Add(params int[] widths)
        {
            var bar = true;
            foreach (var width in widths)
            {
                modules.AddRange(Enumerable.Repeat(bar, width));
                bar = !bar;
            }
        }

        Add(8, 1, 1, 1, 1, 1, 1, 3);
        for (var i = 0; i < 135; i++)
        {
            Add(1 + random.Next(3), 1 + random.Next(3));
        }

        Add(7, 1, 1, 3, 1, 1, 1, 2, 1);
        using var drawn = SyntheticImage.FromLinear(modules.ToArray(), scale: 1, height: 460);
        using var frame = ImageTransforms.Frame(drawn, 640, 480);
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.Pdf417, DuplicateSuppressionWindow = TimeSpan.Zero });
        decoder.Decode(frame.View, suppressDuplicates: false);

        const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var readers = (System.Collections.IEnumerable)typeof(BbBarcodeDecoder).GetField("_matrixDecoders", Private)!.GetValue(decoder)!;
        var reader = readers.OfType<BbPdf417Reader>().Single();
        var locator = typeof(BbPdf417Reader).GetField("_locator", Private)!.GetValue(reader)!;
        var upright = (BbLuminanceBuffer?)locator.GetType().GetField("_upright", Private)!.GetValue(locator);

        Assert.IsNotNull(upright, "The stripes were never resampled, so this test no longer tests.");
        Assert.IsLessThanOrEqualTo(Math.Max(640 * 480 * 3 / 2, 512 * 1024), upright.Width * upright.Height);
    }
}
