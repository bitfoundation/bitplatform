namespace Bit.BbScanner.Tests;

/// <summary>
/// MicroPDF417 beyond the reference round trips: no misreads from other symbols, damage repaired or refused, the
/// angles a live frame reads at, light on dark symbols, and what a result reports.
/// </summary>
/// <remarks>
/// Every symbol here is one BWIPP wrote (see <c>Reference/ReferenceSymbols.json</c>): the test kit has no MicroPDF417
/// encoder, so the decoder is never checked against a reading of the specification shared with it.
/// </remarks>
[TestClass]
public class MicroPdf417Tests
{
    private static BbBarcodeResult? Decode(BbLuminanceBuffer image, bool still, BbBarcodeFormat formats = BbBarcodeFormat.MicroPdf417)
    {
        var options = still ? BbScannerOptions.ForStillImages() : new BbScannerOptions();
        options.Formats = formats;
        using var decoder = new BbBarcodeDecoder(options);
        return decoder.Decode(image.View, suppressDuplicates: false);
    }

    [TestMethod]
    public void NoOtherSymbologyIsReadAsMicroPdf417()
    {
        // MicroPDF417 has no finder or start pattern: it is found through its row address patterns, ten module
        // patterns of three bars like many a character of another symbology. Every other fixture is searched for it
        // with the exhaustive still image settings.
        var misreads = new List<string>();
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat != BbBarcodeFormat.MicroPdf417))
        {
            using var image = symbol.Render(scale: symbol.IsLinear ? 3 : 4);
            if (Decode(image, still: true) is { } result)
            {
                misreads.Add($"{symbol.Id} as {result.Text}");
            }
        }

        Assert.IsEmpty(misreads);
    }

    [TestMethod]
    public void EveryMicroPdf417SymbolIsReportedAsItselfWithEverySymbologyEnabled()
    {
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.MicroPdf417))
        {
            using var image = symbol.Render(scale: 3);
            var result = Decode(image, still: false, BbBarcodeFormat.All);
            var expectedValue = BbBarcodeFormat.MicroPdf417;
            var actualValue = result?.Format;

            Assert.AreEqual(expectedValue, actualValue, symbol.Id);
        }
    }

    [TestMethod]
    [DataRow("micropdf417-1col", 1, 11)]
    [DataRow("micropdf417-3col-short", 3, 6)]
    [DataRow("micropdf417-4col-largest", 4, 44)]
    public void AResultReportsItsSize(string id, int columns, int rows)
    {
        var symbol = ReferenceSymbols.Get(id);
        using var image = symbol.Render(scale: 3);

        var result = Decode(image, still: false);

        Assert.IsNotNull(result);
        Assert.AreEqual(columns, result.Metadata!.Columns);
        Assert.AreEqual(rows, result.Metadata.Rows);
        Assert.AreEqual($"{columns}x{rows}", result.Metadata.SymbolVersion);
        Assert.IsNull(result.Metadata.ErrorCorrectionLevel, "MicroPDF417 has no security level: each size has its own number of check codewords.");
    }

    [TestMethod]
    [DataRow(-5)]
    [DataRow(-2)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(87)]
    [DataRow(182)]
    [DataRow(273)]
    public void LiveFramesReadASymbolHeldNearlyUprightSidewaysOrUpsideDownWithinFourFrames(int degrees)
    {
        foreach (var id in new[] { "micropdf417-2col", "micropdf417-4col" })
        {
            var symbol = ReferenceSymbols.Get(id);
            using var rendered = symbol.Render(scale: 3);
            using var rotated = ImageTransforms.Rotate(rendered, degrees);

            // A live frame tries upright and sideways, and two of the angles near them in turn: within four frames.
            using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.MicroPdf417 });
            string? text = null;
            for (var frame = 0; frame < 4 && text is null; frame++)
            {
                text = decoder.Decode(rotated.View, suppressDuplicates: false)?.Text;
            }

            Assert.AreEqual(symbol.Expected, text, $"{id} at {degrees} degrees");
        }
    }

    [TestMethod]
    public void AStillImageReadsASymbolAtAnyAngle()
    {
        var symbol = ReferenceSymbols.Get("micropdf417-3col");
        using var rendered = symbol.Render(scale: 3);
        var missed = new List<int>();
        for (var degrees = 0; degrees < 360; degrees += 25)
        {
            using var rotated = ImageTransforms.Rotate(rendered, degrees);
            if (Decode(rotated, still: true)?.Text != symbol.Expected)
            {
                missed.Add(degrees);
            }
        }

        Assert.IsEmpty(missed, $"Missed at {string.Join(", ", missed)} degrees.");
    }

    [TestMethod]
    public void TwoSymbolsOfOneSizeSideBySideOrStackedAreBothRead()
    {
        // The lines along one direction read both symbols' rows; were they put in one grid, their codewords would
        // contradict each other and neither would read.
        using var first = SyntheticImage.FromMatrix(MicroPdf417Encoder.Encode("FIRST LABEL", columns: 2), scale: 3);
        using var second = SyntheticImage.FromMatrix(MicroPdf417Encoder.Encode("SECOND ONE", columns: 2), scale: 3);
        foreach (var sideBySide in new[] { true, false })
        {
            using var canvas = ImageTransforms.Frame(first, sideBySide ? (first.Width * 2) + 40 : first.Width + 40, sideBySide ? first.Height + 40 : (first.Height * 2) + 40);
            canvas.Pixels.Fill(255);
            ImageTransforms.Paste(first, canvas, 10, 10);
            ImageTransforms.Paste(second, canvas, sideBySide ? first.Width + 30 : 10, sideBySide ? 10 : first.Height + 30);

            var options = BbScannerOptions.ForStillImages();
            options.Formats = BbBarcodeFormat.MicroPdf417;
            using var decoder = new BbBarcodeDecoder(options);
            var texts = decoder.DecodeAll(canvas.View, maxSymbols: 4, suppressDuplicates: false).Select(r => r.Text).Order(StringComparer.Ordinal);

            Assert.AreEqual("FIRST LABEL | SECOND ONE", string.Join(" | ", texts), sideBySide ? "side by side" : "stacked");
        }
    }

    [TestMethod]
    public void ALightOnDarkSymbolReadsWhenInvertedSymbolsAreAllowed()
    {
        var symbol = ReferenceSymbols.Get("micropdf417-2col");
        using var rendered = symbol.Render(scale: 3);
        using var inverted = ImageTransforms.Invert(rendered);

        Assert.IsNull(Decode(inverted, still: false), "Live settings do not look for inverted symbols.");
        var expectedValue = symbol.Expected;
        var actualValue = Decode(inverted, still: true)?.Text;
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void ABlotIsRepairedByErrorCorrection()
    {
        // A dark blot over a column of codewords in two rows: what the lines read there is wrong or nothing, and the
        // check codewords - 32 in a three column symbol of 26 rows - repair it.
        var symbol = ReferenceSymbols.Get("micropdf417-3col");
        using var image = symbol.Render(scale: 3);
        Blot(image, x: (4 * 3) + (40 * 3), y: (4 * 3) + (10 * 3 * 2), width: 17 * 3, height: 2 * 3 * 2, dark: true);

        var result = Decode(image, still: false);
        var expectedValue = symbol.Expected;
        var actualValue = result?.Text;

        Assert.AreEqual(expectedValue, actualValue);
        Assert.IsGreaterThan(0, result!.Metadata!.ErrorsCorrected ?? 0, "The blot should have cost error correction something.");
    }

    [TestMethod]
    public void ASymbolDamagedBeyondRepairIsNotReported()
    {
        // Half of every row blotted out: far more than the check codewords can repair. Nothing is better than
        // something wrong.
        foreach (var id in new[] { "micropdf417-2col", "micropdf417-4col" })
        {
            var symbol = ReferenceSymbols.Get(id);
            using var image = symbol.Render(scale: 3);
            Blot(image, x: (4 * 3) + (10 * 3), y: 0, width: symbol.Width * 3 / 2, height: image.Height, dark: false);

            Assert.IsNull(Decode(image, still: true), id);
        }
    }

    [TestMethod]
    public void NoiseAndBlankFramesReadNothing()
    {
        using var blank = Blank(640, 480);
        Assert.IsNull(Decode(blank, still: true));

        for (var seed = 0; seed < 4; seed++)
        {
            using var noise = Blank(320, 240);
            SyntheticImage.AddNoise(noise, 255, seed);
            Assert.IsNull(Decode(noise, still: true), $"seed {seed}");
        }
    }

    [TestMethod]
    [DataRow("MICRO", 1)]
    public void TheTestKitEncoderWritesWhatBwippWrites(string content, int columns)
    {
        // The test kit's encoder is what the demo and the decode lab draw samples with; where it compacts the data as
        // BWIPP does - BWIPP chooses its compaction more cleverly for most texts - the symbols must be identical, module
        // for module. A symbol of more columns it writes is checked by reading it, with a reader checked against BWIPP.
        var symbol = ReferenceSymbols.All.Single(s => s.BarcodeFormat == BbBarcodeFormat.MicroPdf417 && s.Expected == content);
        using var reference = symbol.ToMatrix();
        using var encoded = MicroPdf417Encoder.Encode(content, columns, minimumRows: reference.Height / 2);

        Assert.AreEqual(reference.Width, encoded.Width, symbol.Id);
        Assert.AreEqual(reference.Height, encoded.Height, symbol.Id);
        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                Assert.AreEqual(reference[x, y], encoded[x, y], $"{symbol.Id}: module ({x}, {y})");
            }
        }
    }

    [TestMethod]
    [DataRow("A", 0)]
    [DataRow("HELLO WORLD", 1)]
    [DataRow("Mixed case, punctuation & 123", 2)]
    [DataRow("THREE COLUMNS OF UPPERCASE TEXT", 3)]
    [DataRow("Four columns: lower case, digits 0123456789 and symbols !@#", 4)]
    public void TheTestKitEncoderRoundTrips(string content, int columns)
    {
        using var matrix = MicroPdf417Encoder.Encode(content, columns);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 3);
        var expectedValue = content;
        var actualValue = Decode(image, still: false)?.Text;

        Assert.AreEqual(expectedValue, actualValue);
    }

    // "0109501101530003" and "17260101": a GTIN and an expiry date, the GS1 element string of a healthcare label.
    private static readonly int[] Gs1Numeric = [2, 313, 580, 888, 484, 449, 736, 111, 201];

    // "0109501101530003" in text compaction's Mixed sub-mode, two digits to a codeword.
    private static readonly int[] Gs1Mixed = [1, 9, 150, 31, 1, 153, 0, 3];

    [TestMethod]
    public void ACode128EmulationCodewordWithFnc1InFirstPositionMakesAGs1Symbol()
    {
        // 904 implies a latch to numeric compaction, 903 to text compaction's Mixed sub-mode.
        using var numeric = MicroPdf417Encoder.EncodeCodewords([904, .. Gs1Numeric], columns: 2);
        using var image = SyntheticImage.FromMatrix(numeric, scale: 3);
        var result = Decode(image, still: false);

        Assert.IsNotNull(result);
        Assert.AreEqual("010950110153000317260101", result.Text);
        var actualValue = result.Metadata?.IsGs1;
        Assert.IsTrue(actualValue);
        Assert.IsTrue(result.TryGetGs1(out var elements));
        Assert.AreEqual("(01)09501101530003(17)260101", elements.ToBracketedString());

        var mixed = BbPdf417BitStreamParser.DecodeMicroPdf417([1 + 1 + Gs1Mixed.Length, 903, .. Gs1Mixed], 8, 2, 0);
        Assert.IsNotNull(mixed);
        Assert.AreEqual("0109501101530003", mixed.Text);
        Assert.IsTrue(mixed.IsGs1);
    }

    [TestMethod]
    [DataRow(908)]
    [DataRow(910)]
    public void ACode128EmulationCodewordWithoutFnc1InFirstPositionIsNotGs1(int flag)
    {
        var result = BbPdf417BitStreamParser.DecodeMicroPdf417([1 + 1 + Gs1Mixed.Length, flag, .. Gs1Mixed], 8, 2, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual("0109501101530003", result.Text);
        Assert.IsFalse(result.IsGs1);
    }

    [TestMethod]
    [DataRow(909)]
    [DataRow(911)]
    public void ACode128EmulationCodewordCanImplyNumericCompaction(int flag)
    {
        var result = BbPdf417BitStreamParser.DecodeMicroPdf417([1 + 1 + Gs1Numeric.Length, flag, .. Gs1Numeric], 8, 2, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual("010950110153000317260101", result.Text);
        Assert.IsFalse(result.IsGs1);
    }

    [TestMethod]
    [DataRow(905)]
    [DataRow(906)]
    [DataRow(907)]
    [DataRow(912)]
    [DataRow(914)]
    [DataRow(915)]
    [DataRow(920)]
    public void AnEmulationCodewordThisReaderDoesNotInterpretIsNotGuessedAt(int flag)
    {
        // 905 leaves out an AI it implies, and the others open the two-dimensional part of a composite symbol: read
        // as anything else they would be misread.
        Assert.IsNull(BbPdf417BitStreamParser.DecodeMicroPdf417([1 + 1 + Gs1Numeric.Length, flag, .. Gs1Numeric], 8, 2, 0));
    }

    [TestMethod]
    public void Code128EmulationIsMicroPdf417sAlone()
    {
        // In PDF417 the same codewords are reserved.
        Assert.IsNull(BbPdf417BitStreamParser.Decode([1 + 1 + Gs1Numeric.Length, 904, .. Gs1Numeric], 3, 4, 0, 0));
    }

    [TestMethod]
    public void AMicroPdf417ResultReportsWhatTheParserRead()
    {
        int[] codewords = [5, 900, (7 * 30) + 4, 928, 111, 105, 17, 300, 922];
        codewords[0] = codewords.Length;

        var result = BbPdf417BitStreamParser.DecodeMicroPdf417(codewords, 10, 3, 2);

        Assert.IsNotNull(result);
        Assert.AreEqual("HE", result.Text);
        Assert.AreEqual("3x10", result.SymbolVersion);
        Assert.AreEqual(10, result.Rows);
        Assert.AreEqual(3, result.Columns);
        Assert.AreEqual(2, result.ErrorsCorrected);
        Assert.IsNull(result.ErrorCorrectionLevel, "MicroPDF417 has no security level.");
        Assert.AreEqual(5, result.StructuredAppendIndex);
        Assert.IsTrue(result.StructuredAppendIsLast);
        Assert.IsNotNull(result.StructuredAppendFileId);
    }

    [TestMethod]
    [DoNotParallelize]
    public void PipelineLeavesMicroPdf417DisabledWhenARegisteredTableIsNotTheSpecification()
    {
        // MicroPDF417's symbol characters are PDF417's, read with the same table; a table that does not claim to be
        // the specification's turns it off too. The table is process wide, so this puts the specification one back.
#pragma warning disable CS0618 // The placeholder is exactly the kind of table this is about.
        BbPdf417SymbolTable.Register(BbPdf417SymbolTable.CreatePlaceholder());
#pragma warning restore CS0618
        try
        {
            var symbol = ReferenceSymbols.Get("micropdf417-2col");
            using var image = symbol.Render(scale: 3);

            Assert.IsNull(Decode(image, still: true));
        }
        finally
        {
            BbPdf417SymbolTable.Register(BbPdf417SymbolTable.Specification);
        }
    }

    private static BbLuminanceBuffer Blank(int width, int height)
    {
        var image = BbLuminanceBuffer.Rent(width, height);
        image.Pixels.Fill(255);
        return image;
    }

    private static void Blot(BbLuminanceBuffer image, int x, int y, int width, int height, bool dark)
    {
        var pixels = image.Pixels;
        for (var row = Math.Max(0, y); row < Math.Min(image.Height, y + height); row++)
        {
            for (var column = Math.Max(0, x); column < Math.Min(image.Width, x + width); column++)
            {
                pixels[(row * image.Width) + column] = dark ? (byte)0 : (byte)255;
            }
        }
    }
}
