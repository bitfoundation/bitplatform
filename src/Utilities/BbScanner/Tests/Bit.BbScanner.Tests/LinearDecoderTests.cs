namespace Bit.BbScanner.Tests;

/// <summary>Round trips every linear symbology through a rendered image.</summary>
[TestClass]
public class LinearDecoderTests
{
    private static BbSymbolDecodeResult? DecodeMiddleRow(
        IBbRowDecoder decoder, bool[] modules, BbBarcodeFormat formats, int scale = 3)
    {
        using var image = SyntheticImage.FromLinear(modules, scale);
        var row = new BbBitRow(image.Width);
        var binarizer = new BbGlobalHistogramBinarizer();
        var middle = image.Height / 2;

        return binarizer.TryGetBlackRow(image.View, middle, row)
            ? decoder.DecodeRow(middle, row, formats)
            : null;
    }

    [TestMethod]
    [DataRow("HELLO")]
    [DataRow("Blazor-Barcode-Scanner_2026")]
    [DataRow("ABC123def!@#")]
    [DataRow("0123456789")]
    public void Code128RoundTrips(string content)
    {
        var result = DecodeMiddleRow(new BbCode128Reader(), LinearEncoders.Code128(content), BbBarcodeFormat.Code128);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Code128, result.Format);
        Assert.AreEqual(content, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("CODE 93")]
    [DataRow("1234567890")]
    [DataRow("Mixed case, punctuation & symbols: ~{}[]@`")]
    public void Code93RoundTrips(string content)
    {
        var result = DecodeMiddleRow(new BbCode93Reader(), LinearEncoders.Code93(content), BbBarcodeFormat.Code93);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Code93, result.Format);
        Assert.AreEqual(content, result.Payload.Text);
    }

    [TestMethod]
    public void Code93ReadsEveryAsciiCharacter()
    {
        // The full ASCII table goes through the four shift characters; every one of the 128 has to come back.
        var all = new string(Enumerable.Range(0, 128).Select(i => (char)i).ToArray());
        foreach (var chunk in all.Chunk(16).Select(c => new string(c)))
        {
            var result = DecodeMiddleRow(new BbCode93Reader(), LinearEncoders.Code93(chunk), BbBarcodeFormat.Code93);
            Assert.AreEqual(chunk, result?.Payload.Text, $"Characters {(int)chunk[0]} to {(int)chunk[^1]}");
        }
    }

    [TestMethod]
    public void Code93RejectsABadCheckCharacter()
    {
        // Swap the last data character's bars for another character's; both checks then disagree.
        var good = LinearEncoders.Code93("CHECKED");
        var bad = LinearEncoders.Code93("CHECKEE");
        var spliced = good[..^28].Concat(bad[^28..]).ToArray();

        Assert.IsNull(DecodeMiddleRow(new BbCode93Reader(), spliced, BbBarcodeFormat.Code93));
    }

    [TestMethod]
    [DataRow("0123456789", 'A', 'B')]
    [DataRow("-$:/.+", 'C', 'D')]
    [DataRow("40156", 'D', 'A')]
    public void CodabarRoundTrips(string data, char start, char stop)
    {
        var result = DecodeMiddleRow(new BbCodabarReader(), LinearEncoders.Codabar(data, start, stop), BbBarcodeFormat.Codabar);
        var kept = DecodeMiddleRow(new BbCodabarReader(keepStartStop: true), LinearEncoders.Codabar(data, start, stop), BbBarcodeFormat.Codabar);

        Assert.AreEqual(data, result?.Payload.Text);
        Assert.AreEqual($"{start}{data}{stop}", kept?.Payload.Text);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void CodabarReadsBothWideToNarrowRatios(int ratio)
    {
        var result = DecodeMiddleRow(new BbCodabarReader(), LinearEncoders.Codabar("31234000123457", wideRatio: ratio), BbBarcodeFormat.Codabar);

        Assert.AreEqual("31234000123457", result?.Payload.Text);
    }

    [TestMethod]
    [DataRow(2, 3, -1)]
    [DataRow(2, 3, 1)]
    [DataRow(2, 4, -1)]
    [DataRow(2, 4, 1)]
    [DataRow(3, 4, -2)]
    [DataRow(3, 4, 2)]
    public void CodabarReadsBarsPrintedWideOrNarrow(int ratio, int scale, int barGain)
    {
        // Ink spread draws every bar wider and every space narrower by the same amount. Fitted as one narrow and one
        // wide width, a pixel of it read as misfit and lost the symbol at a two to one ratio.
        using var image = SyntheticImage.FromLinearWithBarGain(LinearEncoders.Codabar("31234000123457", wideRatio: ratio), scale, barGain);
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.Codabar, DuplicateSuppressionWindow = TimeSpan.Zero });

        Assert.AreEqual("31234000123457", decoder.Decode(image.View, suppressDuplicates: false)?.Text);
    }

    [TestMethod]
    public void CodabarNeedsThreeDataCharacters()
    {
        Assert.IsNull(DecodeMiddleRow(new BbCodabarReader(), LinearEncoders.Codabar("12"), BbBarcodeFormat.Codabar));
        Assert.IsNotNull(DecodeMiddleRow(new BbCodabarReader(), LinearEncoders.Codabar("123"), BbBarcodeFormat.Codabar));
    }

    [TestMethod]
    [DataRow("0000000000000")]
    [DataRow("0952123454321")]
    [DataRow("1234567890123")]
    [DataRow("9999999999999")]
    public void DataBarRoundTrips(string digits)
    {
        var gtin = digits + LinearEncoders.EanCheckDigit(digits);
        var result = DecodeMiddleRow(new BbDataBarReader(), LinearEncoders.DataBar(gtin), BbBarcodeFormat.DataBar);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.DataBar, result.Format);
        Assert.AreEqual("01" + gtin, result.Payload.Text);
        Assert.IsTrue(result.Payload.IsGs1);
    }

    [TestMethod]
    public void TheDataBarTestEncoderDrawsWhatTheIndependentEncoderDraws()
    {
        // The test kit's encoder and BWIPP must put down the same modules for the same GTIN; otherwise the round trips
        // above would only prove that the decoder agrees with the test kit.
        foreach (var symbol in ReferenceSymbols.All.Where(s => s.BarcodeFormat == BbBarcodeFormat.DataBar && s.IsLinear))
        {
            var expected = SyntheticImage.ExpandWidths(symbol.Widths);
            var actual = LinearEncoders.DataBar(symbol.Expected![2..]);

            // BWIPP's widths start with the left guard's space; the test kit leaves it to the quiet zone.
            CollectionAssert.AreEqual(expected.SkipWhile(m => !m).ToArray(), actual.SkipWhile(m => !m).ToArray(), symbol.Id);
        }
    }

    [TestMethod]
    public void DataBarReportsBothGuardBarsAsItsEnds()
    {
        // The test kit draws from the left guard's bar to the right guard's bar, twelve modules of quiet zone either
        // side, three pixels a module.
        var modules = LinearEncoders.DataBar("09521234543213");
        var result = DecodeMiddleRow(new BbDataBarReader(), modules, BbBarcodeFormat.DataBar);

        Assert.IsNotNull(result);
        Assert.AreEqual(12 * 3, result.Points[0].X);
        Assert.AreEqual((12 + modules.Length) * 3, result.Points[1].X);
    }

    [TestMethod]
    public void DataBarIsNotReadWithoutItsRightGuardBar()
    {
        var modules = LinearEncoders.DataBar("09521234543213");
        modules[^1] = false;

        Assert.IsNull(DecodeMiddleRow(new BbDataBarReader(), modules, BbBarcodeFormat.DataBar));
    }

    [TestMethod]
    public void DataBarPrintedAgainstOtherInkIsMissedRatherThanRisked()
    {
        // A module of light beside a module of bar is what the inside of a symbol is made of too, so the reader wants a
        // module and a half of light beyond each outermost bar: one module is refused on either side, two are read.
        var symbol = LinearEncoders.DataBar("09521234543213");

        Assert.IsNull(DecodeMiddleRow(new BbDataBarReader(), [true, true, false, .. symbol], BbBarcodeFormat.DataBar));
        Assert.IsNull(DecodeMiddleRow(new BbDataBarReader(), [.. symbol, false, true, true], BbBarcodeFormat.DataBar));
        Assert.IsNotNull(DecodeMiddleRow(new BbDataBarReader(), [true, true, false, false, .. symbol], BbBarcodeFormat.DataBar));
        Assert.IsNotNull(DecodeMiddleRow(new BbDataBarReader(), [.. symbol, false, false, true, true], BbBarcodeFormat.DataBar));
    }

    [TestMethod]
    public void DataBarRejectsAChangedCharacter()
    {
        // Swap a module between two elements of the first character: the widths no longer match the checksum the
        // finder patterns carry.
        var modules = LinearEncoders.DataBar("09521234543213");
        var firstSpace = Array.IndexOf(modules, false, 1);
        var nextBar = Array.IndexOf(modules, true, firstSpace);
        modules[nextBar] = false;

        Assert.IsNull(DecodeMiddleRow(new BbDataBarReader(), modules, BbBarcodeFormat.DataBar));
    }

    [TestMethod]
    public void DataBarIsReadTurnedAndUpsideDown()
    {
        using var image = SyntheticImage.FromLinear(LinearEncoders.DataBar("09521234543213"), scale: 3, height: 90);
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.DataBar, DuplicateSuppressionWindow = TimeSpan.Zero });
        foreach (var degrees in new[] { 0, 12, -12, 25, 180, 192 })
        {
            using var rotated = ImageTransforms.Rotate(image, degrees);
            Assert.AreEqual("0109521234543213", decoder.Decode(rotated.View, suppressDuplicates: false)?.Text, $"{degrees} degrees");
        }
    }

    [TestMethod]
    [DataRow("HELLO")]
    [DataRow("ABC-123")]
    [DataRow("CODE39 TEST")]
    public void Code39RoundTrips(string content)
    {
        var result = DecodeMiddleRow(new BbCode39Reader(), LinearEncoders.Code39(content), BbBarcodeFormat.Code39);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Code39, result.Format);
        Assert.AreEqual(content, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("400638133393")]
    [DataRow("978020137962")]
    [DataRow("590123412345")]
    public void Ean13RoundTrips(string withoutCheck)
    {
        var digits = withoutCheck + LinearEncoders.EanCheckDigit(withoutCheck);
        var result = DecodeMiddleRow(new BbEanUpcReader(), LinearEncoders.Ean13(digits), BbBarcodeFormat.Ean13);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Ean13, result.Format);
        Assert.AreEqual(digits, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("9638507")]
    [DataRow("5512345")]
    public void Ean8RoundTrips(string withoutCheck)
    {
        var digits = withoutCheck + LinearEncoders.EanCheckDigit(withoutCheck);
        var result = DecodeMiddleRow(new BbEanUpcReader(), LinearEncoders.Ean8(digits), BbBarcodeFormat.Ean8);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Ean8, result.Format);
        Assert.AreEqual(digits, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("03600029145")]
    [DataRow("01234567890")]
    public void UpcARoundTrips(string withoutCheck)
    {
        var digits = withoutCheck + LinearEncoders.EanCheckDigit("0" + withoutCheck);
        var result = DecodeMiddleRow(new BbEanUpcReader(), LinearEncoders.UpcA(digits), BbBarcodeFormat.UpcA);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.UpcA, result.Format);
        Assert.AreEqual(digits, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("01234565")]
    [DataRow("04252614")]
    public void UpcERoundTrips(string digits)
    {
        // The parity of a UPC-E symbol encodes its check digit, so only self-consistent values
        // can be encoded at all; the expanded UPC-A form is what the decoder verifies.
        var expanded = BbEanUpcReader.ExpandUpcEToUpcA(digits);
        Assert.IsNotNull(expanded);
        Assert.AreEqual(expanded[^1], LinearEncoders.EanCheckDigit(expanded[..^1]));

        var result = DecodeMiddleRow(new BbEanUpcReader(), LinearEncoders.UpcE(digits), BbBarcodeFormat.UpcE);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.UpcE, result.Format);
        Assert.AreEqual(digits, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("1234567890")]
    [DataRow("00123456")]
    [DataRow("98765432109876")]
    public void ItfRoundTrips(string digits)
    {
        var result = DecodeMiddleRow(new BbItfReader(), LinearEncoders.Itf(digits), BbBarcodeFormat.Itf, scale: 4);

        Assert.IsNotNull(result);
        Assert.AreEqual(BbBarcodeFormat.Itf, result.Format);
        Assert.AreEqual(digits, result.Payload.Text);
    }

    [TestMethod]
    [DataRow("!R")]
    [DataRow("0050")]
    public void Code128CheckCharacterOfValue102IsNotReportedAsAGroupSeparator(string content)
    {
        // These contents have a modulo 103 check character of 102, the value of FNC1. The check
        // character is never data, so nothing may be appended for it.
        var modules = LinearEncoders.Code128(content);
        using var image = SyntheticImage.FromLinear(modules, scale: 3, height: 40);

        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.Code128, DuplicateSuppressionWindow = TimeSpan.Zero });
        var result = decoder.Decode(image.View, suppressDuplicates: false);

        Assert.IsNotNull(result);
        Assert.AreEqual(content, result.Text);
        Assert.DoesNotContain((char)29, result.Text);
    }

    [TestMethod]
    public void Code128ReportsTheScanLinePosition()
    {
        var modules = LinearEncoders.Code128("POSITION");
        var result = DecodeMiddleRow(new BbCode128Reader(), modules, BbBarcodeFormat.Code128);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Points.Length);
        Assert.IsTrue(result.Points[0].X < result.Points[1].X);
        Assert.AreEqual(result.Points[0].Y, result.Points[1].Y);
    }

    [TestMethod]
    public void DisabledFormatsAreNotDecoded()
    {
        var modules = LinearEncoders.Code128("SKIPPED");
        Assert.IsNull(DecodeMiddleRow(new BbCode128Reader(), modules, BbBarcodeFormat.Ean13));
    }

    [TestMethod]
    public void RandomNoiseDoesNotProduceAResult()
    {
        var random = new Random(99);
        var modules = new bool[400];
        for (var i = 0; i < modules.Length; i++)
        {
            modules[i] = random.Next(2) == 0;
        }

        Assert.IsNull(DecodeMiddleRow(new BbCode128Reader(), modules, BbBarcodeFormat.Code128));
        Assert.IsNull(DecodeMiddleRow(new BbEanUpcReader(), modules, BbBarcodeFormat.All));
    }
}
