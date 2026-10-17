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
