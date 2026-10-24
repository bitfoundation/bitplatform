namespace Bit.BbScanner.Tests;

/// <summary>
/// Aztec Code through the whole pipeline: every symbol size class, rotation, light on dark, damage within what the
/// error correction repairs, and the misreads it must not make. The BWIPP fixtures in <c>Reference</c> check the
/// character modes against an independent encoder; these round trip the test kit's own.
/// </summary>
[TestClass]
public class AztecDecoderTests
{
    private static BbBarcodeResult? Decode(BbLuminanceBuffer image, BbScannerOptions? options = null, BbBarcodeFormat formats = BbBarcodeFormat.Aztec)
    {
        options ??= new BbScannerOptions();
        options.Formats = formats;
        options.DuplicateSuppressionWindow = TimeSpan.Zero;
        using var decoder = new BbBarcodeDecoder(options);
        return decoder.Decode(image.View, suppressDuplicates: false);
    }

    [TestMethod]
    [DataRow(true, 1, "AZTEC")]
    [DataRow(true, 2, "COMPACT TWO")]
    [DataRow(true, 3, "Compact three: 8 bit codewords")]
    [DataRow(true, 4, "Compact four layers, the largest compact symbol.")]
    [DataRow(false, 1, "FULL")]
    [DataRow(false, 5, "Full range with five layers")]
    [DataRow(false, 9, "Nine layers: the first of the ten bit codewords, and two reference grid lines.")]
    [DataRow(false, 23, "")]
    public void RoundTripsEverySizeClass(bool compact, int layers, string content)
    {
        // The larger symbols need enough data to be chosen; the size is forced, the rest fills it.
        if (content.Length == 0)
        {
            content = string.Concat(Enumerable.Repeat("Twelve bit codewords. ", 30));
        }

        using var matrix = AztecEncoder.Encode(content, compact: compact, layers: layers);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 3);

        var result = Decode(image);

        Assert.IsNotNull(result, $"{(compact ? "compact" : "full range")} {layers}");
        Assert.AreEqual(BbBarcodeFormat.Aztec, result.Format);
        Assert.AreEqual(content, result.Text);
        StringAssert.StartsWith(result.Metadata?.SymbolVersion, compact ? "Compact" : "Full range");
    }

    [TestMethod]
    public void ReadsTheDefaultLiveSettingsAtEveryQuarterTurnAndBetween()
    {
        using var matrix = AztecEncoder.Encode("M1PASSENGER/TEST EABC123 LHRJFKBA 0117 123Y012A0042 100");
        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
        var failures = new List<int>();
        for (var degrees = 0; degrees < 360; degrees += 30)
        {
            using var rotated = ImageTransforms.Rotate(image, degrees + 7);
            if (Decode(rotated)?.Text != "M1PASSENGER/TEST EABC123 LHRJFKBA 0117 123Y012A0042 100")
            {
                failures.Add(degrees + 7);
            }
        }

        Assert.IsEmpty(failures, $"Not read at {string.Join(", ", failures)} degrees.");
    }

    [TestMethod]
    public void ReadsASymbolSeenAtAnAngle()
    {
        // A small symbol has no reference grid to fit perspective from; its outline does it.
        using var matrix = AztecEncoder.Encode("Seen at an angle", compact: true, layers: 3);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
        using var tilted = ImageTransforms.Tilt(image, 35);

        Assert.AreEqual("Seen at an angle", Decode(tilted, BbScannerOptions.ForStillImages())?.Text);
    }

    [TestMethod]
    public void ReadsALargeSymbolSeenAtAnAngleThroughItsReferenceGrid()
    {
        var content = string.Concat(Enumerable.Repeat("Reference grid. ", 20));
        using var matrix = AztecEncoder.Encode(content, compact: false, layers: 16);
        using var image = SyntheticImage.FromMatrix(matrix, scale: 3);
        using var tilted = ImageTransforms.Tilt(image, 30);
        using var rotated = ImageTransforms.Rotate(tilted, 23);

        Assert.AreEqual(content, Decode(rotated, BbScannerOptions.ForStillImages())?.Text);
    }

    [TestMethod]
    public void ReadsLightOnDarkWhenInvertedSymbolsAreAllowed()
    {
        using var matrix = AztecEncoder.Encode("ON A PHONE SCREEN");
        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
        using var inverted = ImageTransforms.Invert(image);

        Assert.IsNull(Decode(inverted, new BbScannerOptions { AllowInverted = false }));
        Assert.AreEqual("ON A PHONE SCREEN", Decode(inverted, new BbScannerOptions { AllowInverted = true })?.Text);
    }

    [TestMethod]
    public void RepairsDamageWithinItsErrorCorrection()
    {
        using var matrix = AztecEncoder.Encode("DAMAGED BUT READABLE", errorCorrectionPercent: 50, compact: false, layers: 3);

        // Spoil a short run of data modules in the outermost layer, well clear of the bullseye and the mode message.
        for (var x = 2; x < 9; x++)
        {
            matrix[x, 0] = !matrix[x, 0];
        }

        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
        var result = Decode(image);

        Assert.AreEqual("DAMAGED BUT READABLE", result?.Text);
        Assert.IsGreaterThan(0, result!.Metadata!.ErrorsCorrected ?? 0);
    }

    [TestMethod]
    public void TooMuchDamageIsNotReadAsSomethingElse()
    {
        using var matrix = AztecEncoder.Encode("SPOILT", errorCorrectionPercent: 10, compact: true, layers: 2);
        var random = new Random(7);
        var size = matrix.Width;
        var centre = size / 2;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                // Everything but the bullseye and the mode message, flipped at random.
                if (Math.Max(Math.Abs(x - centre), Math.Abs(y - centre)) > 5 && random.Next(3) == 0)
                {
                    matrix[x, y] = !matrix[x, y];
                }
            }
        }

        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);

        Assert.IsNull(Decode(image, BbScannerOptions.ForStillImages()));
    }

    [TestMethod]
    public void OtherSymbologiesAndNoiseAreNotReadAsAztec()
    {
        using var qr = QrEncoder.Encode("NOT AN AZTEC SYMBOL");
        using var dataMatrix = DataMatrixEncoder.Encode("NOT AN AZTEC SYMBOL");
        using var pdf417 = Pdf417Encoder.Encode("NOT AN AZTEC SYMBOL");
        foreach (var matrix in new[] { qr, dataMatrix, pdf417 })
        {
            using var image = SyntheticImage.FromMatrix(matrix, scale: 4);
            for (var degrees = 0; degrees < 360; degrees += 45)
            {
                using var rotated = ImageTransforms.Rotate(image, degrees);
                Assert.IsNull(Decode(rotated, BbScannerOptions.ForStillImages()), $"Read as Aztec at {degrees} degrees.");
            }
        }

        for (var seed = 0; seed < 20; seed++)
        {
            using var noise = BbLuminanceBuffer.Rent(320, 240);
            noise.Pixels.Fill(128);
            SyntheticImage.AddNoise(noise, 127, seed);
            Assert.IsNull(Decode(noise, BbScannerOptions.ForStillImages()), $"Noise {seed} read as Aztec.");
        }
    }

    [TestMethod]
    public void IsOnlyReadWhenAsked()
    {
        using var matrix = AztecEncoder.Encode("FORMATS");
        using var image = SyntheticImage.FromMatrix(matrix, scale: 4);

        Assert.IsNull(Decode(image, formats: BbBarcodeFormat.All & ~BbBarcodeFormat.Aztec));
        Assert.AreEqual("FORMATS", Decode(image, formats: BbBarcodeFormat.All)?.Text);
        Assert.IsTrue(BbBarcodeFormat.AllMatrix.HasFlag(BbBarcodeFormat.Aztec));
    }

    [TestMethod]
    public void ReportsTheCornersOfTheWholeSymbol()
    {
        const int Scale = 4;
        using var matrix = AztecEncoder.Encode("CORNERS", compact: true, layers: 2);
        using var image = SyntheticImage.FromMatrix(matrix, scale: Scale);

        var result = Decode(image);

        // Upright, the corners are those of the module grid, clockwise from the top left, inside the quiet zone.
        Assert.IsNotNull(result);
        var quiet = 4 * Scale;
        var far = quiet + (matrix.Width * Scale);
        (float X, float Y)[] expected = [(quiet, quiet), (far, quiet), (far, far), (quiet, far)];
        for (var i = 0; i < 4; i++)
        {
            Assert.AreEqual(expected[i].X, result.Corners[i].X, 1.5f, $"corner {i}");
            Assert.AreEqual(expected[i].Y, result.Corners[i].Y, 1.5f, $"corner {i}");
        }
    }

    [TestMethod]
    public void ReadsEveryAztecSymbolInAnImage()
    {
        using var left = AztecEncoder.Encode("LEFT TICKET");
        using var right = AztecEncoder.Encode("RIGHT TICKET");
        using var leftImage = SyntheticImage.FromMatrix(left, scale: 4);
        using var rightImage = SyntheticImage.FromMatrix(right, scale: 4);
        using var sheet = BbLuminanceBuffer.Rent(leftImage.Width + rightImage.Width, Math.Max(leftImage.Height, rightImage.Height));
        sheet.Pixels.Fill(255);
        for (var y = 0; y < leftImage.Height; y++)
        {
            leftImage.Pixels.Slice(y * leftImage.Width, leftImage.Width).CopyTo(sheet.Pixels.Slice(y * sheet.Width, leftImage.Width));
            rightImage.Pixels.Slice(y * rightImage.Width, rightImage.Width).CopyTo(sheet.Pixels.Slice((y * sheet.Width) + leftImage.Width, rightImage.Width));
        }

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.Aztec, DuplicateSuppressionWindow = TimeSpan.Zero });
        var values = decoder.DecodeAll(sheet.View, maxSymbols: 4).Select(r => r.Text).Order().ToList();

        CollectionAssert.AreEqual(new[] { "LEFT TICKET", "RIGHT TICKET" }, values);
    }
    [TestMethod]
    public void ReedSolomonNeverClaimsToCorrectMoreThanItsCheckCodewordsAllow()
    {
        // Three check codewords locate one error. With an odd count the locator could come out a degree too high,
        // and two errors came back "corrected" as two - into a valid codeword that was never written.
        var field = new BbGenericGF(0x43, 64, 1);
        var encoder = new ReedSolomonEncoder(field);
        var decoder = new BbReedSolomonDecoder(field);
        var random = new Random(5);
        var overclaimed = 0;
        for (var trial = 0; trial < 5000; trial++)
        {
            var block = new int[40];
            for (var i = 0; i < 37; i++)
            {
                block[i] = random.Next(64);
            }

            encoder.Encode(block, 3);
            var first = random.Next(40);
            var second = (first + 1 + random.Next(39)) % 40;
            block[first] ^= 1 + random.Next(63);
            block[second] ^= 1 + random.Next(63);
            if (decoder.TryDecode(block, 3, out var corrected) && corrected > 1)
            {
                overclaimed++;
            }
        }

        Assert.AreEqual(0, overclaimed);
    }

    [TestMethod]
    public void ASymbolWithFewCheckCodewordsIsRefusedRatherThanMisreadWhenDamaged()
    {
        // An encoder at its lowest level writes three check codewords; in six bit codewords they cannot tell a block
        // repaired from one moved onto another codeword, so damage has to be refused, not "corrected".
        // The test kit gives a symbol's spare codewords to error correction, so the content is lengthened until only the
        // three check codewords of the lowest level are left: three of forty is 7%.
        string? content = null;
        BbBitMatrix? found = null;
        for (var length = 36; length <= 48 && content is null; length++)
        {
            var candidate = new string('M', length);
            BbBitMatrix encoded;
            try
            {
                encoded = AztecEncoder.Encode(candidate, errorCorrectionPercent: 0, compact: true, layers: 2);
            }
            catch (ArgumentException)
            {
                break;
            }

            using var clean = SyntheticImage.FromMatrix(encoded, 4);
            var read = Decode(clean, BbScannerOptions.ForStillImages());
            if (read?.Text == candidate && read.Metadata?.ErrorCorrectionLevel == "7%")
            {
                (content, found) = (candidate, encoded);
            }
            else
            {
                encoded.Dispose();
            }
        }

        Assert.IsNotNull(found, "No compact two layer symbol with three check codewords was found, so this test no longer tests.");
        using var symbol = found;

        var random = new Random(9);
        var centre = symbol.Width / 2;
        var wrong = new List<string>();
        for (var trial = 0; trial < 200; trial++)
        {
            // Two modules of the data layers, outside the eleven by eleven core.
            using var damaged = new BbBitMatrix(symbol.Width, symbol.Height);
            for (var y = 0; y < symbol.Height; y++)
            {
                for (var x = 0; x < symbol.Width; x++)
                {
                    damaged[x, y] = symbol[x, y];
                }
            }

            for (var flipped = 0; flipped < 2;)
            {
                var x = random.Next(symbol.Width);
                var y = random.Next(symbol.Height);
                if (Math.Max(Math.Abs(x - centre), Math.Abs(y - centre)) > 5)
                {
                    damaged[x, y] = !damaged[x, y];
                    flipped++;
                }
            }

            using var image = SyntheticImage.FromMatrix(damaged, 4);
            if (Decode(image, BbScannerOptions.ForStillImages())?.Text is { } text && text != content)
            {
                wrong.Add(text);
            }
        }

        Assert.IsEmpty(wrong, string.Join(" | ", wrong));
    }

    [TestMethod]
    public void AModeMessageRepairedIntoTooManyDataCodewordsIsRefused()
    {
        // A mode message repaired into a data codeword count one or two too high still decodes - the block's real check
        // codewords satisfy a subset of their own roots - and they come back as characters on the end of the text. Before
        // the next root was asked for, 13 of the 17 independently encoded fixtures did that. Each fixture's own count is
        // found by trying them all without a repair - which is why the largest, whose counts run to a thousand, are left out -
        // and must still read with one.
        var decoder = typeof(BbAztecReader).Assembly.GetType("Bit.BbScanner.BbAztecDecoder", throwOnError: true)!;
        const System.Reflection.BindingFlags Internal = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var decode = decoder.GetMethod("Decode", Internal)!;
        var size = decoder.GetMethod("Size", Internal)!;
        var wrong = new List<string>();
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.Aztec && s.Width < 45 && !s.Id.StartsWith("aztec-rune", StringComparison.Ordinal)))
        {
            using var grid = symbol.ToMatrix();

            // A 19 module symbol is compact with two layers or full range with one; whichever reads is the one it is.
            (bool Compact, int Layers, int Data) found = default;
            for (var layers = 1; layers <= 32 && found.Data == 0; layers++)
            {
                foreach (var compact in layers <= 4 ? [true, false] : new[] { false })
                {
                    if ((int)size.Invoke(null, [compact, layers])! == grid.Width &&
                        Enumerable.Range(1, 2048).FirstOrDefault(d => Read(compact, layers, d, modeErrors: 0) == symbol.Expected) is var data and > 0)
                    {
                        found = (compact, layers, data);
                        break;
                    }
                }
            }

            Assert.AreNotEqual(0, found.Data, $"{symbol.Id} does not read with any data codeword count.");
            Assert.AreEqual(symbol.Expected, Read(found.Compact, found.Layers, found.Data, modeErrors: 1), $"{symbol.Id} with a repaired mode message.");
            foreach (var overstated in new[] { found.Data + 1, found.Data + 2 })
            {
                if (Read(found.Compact, found.Layers, overstated, modeErrors: 1) is { } text && text != symbol.Expected)
                {
                    wrong.Add($"{symbol.Id} with {overstated} data codewords for {found.Data}: {text}");
                }
            }

            string? Read(bool compact, int layers, int dataCodewords, int modeErrors) =>
                (decode.Invoke(null, [grid, compact, layers, dataCodewords, modeErrors]) as BbDecoderResult)?.Text;
        }

        Assert.IsEmpty(wrong, string.Join(" | ", wrong));
    }

    [TestMethod]
    public void AnImageTiledWithBullseyesIsGivenUpOnQuickly()
    {
        // Every sighting of a bullseye is checked against those already found, so a picked file tiled with thousands
        // of them - which no photograph of real symbols is - cost the square of their number: 44 seconds for this one,
        // 0.4 once the search stops at a few dozen.
        const int Period = 8;
        const int Modules = 1500;
        using var tiles = new BbBitMatrix(Modules, Modules);
        for (var y = 0; y < Modules; y++)
        {
            for (var x = 0; x < Modules; x++)
            {
                var ring = Math.Max(Math.Abs((x % Period) - 4), Math.Abs((y % Period) - 4));
                tiles[x, y] = (ring & 1) == 0;
            }
        }

        using var image = SyntheticImage.FromMatrix(tiles, 2, quietZoneModules: 0);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Decode(image, BbScannerOptions.ForStillImages());
        watch.Stop();

        Assert.IsLessThan(TimeSpan.FromSeconds(5), watch.Elapsed, $"Took {watch.Elapsed}.");
    }

    [TestMethod]
    public void TheOnesThatPadOutATwelveBitCodewordAreNotReadAsABinaryShift()
    {
        // Twelve bit codewords leave up to eleven padding ones in the last one, which read as a binary shift of 31
        // bytes the symbol cannot hold.
        var content = new string('A', 304) + "Z";
        using var symbol = AztecEncoder.Encode(content, compact: false, layers: 23);
        using var image = SyntheticImage.FromMatrix(symbol, 3);

        Assert.AreEqual(content, Decode(image, BbScannerOptions.ForStillImages())?.Text);
    }
    [TestMethod]
    [DataRow("aztec-compact-1")]
    [DataRow("aztec-compact-4")]
    [DataRow("aztec-full-4")]
    [DataRow("aztec-full-12")]
    [DataRow("aztec-boarding-pass")]
    [DataRow("aztec-rune-25")]
    public void AMirroredSymbolReadsAtEveryQuarterTurn(string id)
    {
        // Printed on the back of a transparent label, or seen through one, a symbol is its own mirror image: its
        // orientation modules match no turn of the upright pattern, and its layers run the other way round.
        var symbol = ReferenceSymbols.Get(id);
        using var grid = symbol.ToMatrix();
        using var mirrored = new BbBitMatrix(grid.Width, grid.Height);
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                mirrored[grid.Width - 1 - x, y] = grid[x, y];
            }
        }

        using var rendered = SyntheticImage.FromMatrix(mirrored, 4);
        var failures = new List<string>();
        for (var degrees = 7; degrees < 360; degrees += 90)
        {
            using var rotated = ImageTransforms.Rotate(rendered, degrees);
            foreach (var (name, options) in new[] { ("live", new BbScannerOptions()), ("still", BbScannerOptions.ForStillImages()) })
            {
                if (Decode(rotated, options)?.Text != symbol.Expected)
                {
                    failures.Add($"{degrees} degrees, {name}");
                }
            }
        }

        Assert.IsEmpty(failures, $"{id} mirrored was not read at: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ARuneIsReadAsItsNumberAndOnlyWithAQuietZone()
    {
        // A rune is the bare compact core: its mode message carries a number, and outside it there is nothing.
        var symbol = ReferenceSymbols.Get("aztec-rune-170");
        using var grid = symbol.ToMatrix();
        using var rendered = SyntheticImage.FromMatrix(grid, 4);
        var read = Decode(rendered, BbScannerOptions.ForStillImages());
        Assert.AreEqual("170", read?.Text);
        Assert.AreEqual("Rune", read!.Metadata!.SymbolVersion);
        CollectionAssert.AreEqual(new byte[] { 170 }, read.RawBytes.ToArray());

        // The same core with data around it, where a compact symbol's first layer would be, is not a rune: it is a
        // compact symbol whose mode message does not decode.
        using var crowded = new BbBitMatrix(grid.Width + 4, grid.Height + 4);
        var random = new Random(8);
        for (var y = 0; y < crowded.Height; y++)
        {
            for (var x = 0; x < crowded.Width; x++)
            {
                var inside = x >= 2 && y >= 2 && x < grid.Width + 2 && y < grid.Height + 2;
                crowded[x, y] = inside ? grid[x - 2, y - 2] : random.Next(2) == 0;
            }
        }

        using var crowdedImage = SyntheticImage.FromMatrix(crowded, 4);
        Assert.IsNull(Decode(crowdedImage, BbScannerOptions.ForStillImages()));
    }
}
