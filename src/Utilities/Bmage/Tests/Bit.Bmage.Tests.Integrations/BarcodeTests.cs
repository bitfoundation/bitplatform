using Bit.BbScanner;
using Bit.BbScanner.TestKit;
using Bit.Bmage.BbScanner;

namespace Bit.Bmage.Tests.Integrations;

[TestClass]
public sealed class BarcodeTests
{
    private const string Url = "https://bitplatform.dev/bmage?case=1234567890";

    /// <summary>A QR code of <paramref name="scale"/> pixels a module on a wider page, blurred as a camera out of focus would.</summary>
    internal static ImageBuffer Qr(int scale, double blur)
    {
        var symbol = SyntheticImage.FromMatrix(QrEncoder.Encode(Url), scale, 4);
        var page = ImageTransforms.Frame(symbol, symbol.Width * 3, symbol.Height * 2);
        symbol.Dispose();
        if (blur > 0)
        {
            var blurred = ImageTransforms.Blur(page, blur);
            page.Dispose();
            page = blurred;
        }
        try
        {
            return ImageBuffer.FromRgba(ImageTransforms.ToRgba(page), page.Width, page.Height);
        }
        finally
        {
            page.Dispose();
        }
    }

    private static ImageBuffer Code128(int scale, double blur)
    {
        var symbol = SyntheticImage.FromLinear(LinearEncoders.Code128("BMAGE-1234567"), scale, 40);
        var page = ImageTransforms.Frame(symbol, symbol.Width + 100, symbol.Height * 3);
        symbol.Dispose();
        var blurred = ImageTransforms.Blur(page, blur);
        page.Dispose();
        try
        {
            return ImageBuffer.FromRgba(ImageTransforms.ToRgba(blurred), blurred.Width, blurred.Height);
        }
        finally
        {
            blurred.Dispose();
        }
    }

    [TestMethod]
    public void ASharpCodeReadsAsItIs()
    {
        using var image = Qr(scale: 3, blur: 0);
        var direct = image.Decode();
        Assert.HasCount(1, direct);
        Assert.AreEqual(Url, direct[0].Text);

        var read = image.ReadBarcodes();
        Assert.IsTrue(read.Found);
        Assert.AreEqual(BarcodePreprocessing.None, read.Preprocessing);
        Assert.AreEqual(1, read.ScaleX);
        Assert.AreEqual(1, read.ScaleY);
        Assert.AreSame(read.Barcodes[0].Corners, read.CornersOf(read.Barcodes[0]));
    }

    [TestMethod]
    public void ASoftQrCodeThatDoesNotReadAsItIsReadsSharpened()
    {
        using var image = Qr(scale: 2, blur: 1);
        Assert.IsEmpty(image.Decode(), "The fixture is one the decoders cannot read as it is.");
        using var before = image.Clone();

        var read = image.ReadBarcodes();
        Assert.IsTrue(read.Found);
        Assert.AreEqual(BarcodePreprocessing.Sharpen, read.Preprocessing);
        Assert.AreEqual(Url, read.Barcodes[0].Text);
        Assert.AreEqual(0, MaxDifference(before, image), "Reading never changes the image it was given.");
    }

    [TestMethod]
    public void EnlargingReadsASoftSmallCodeAndItsCornersMapBack()
    {
        using var image = Qr(scale: 2, blur: 1);
        var read = image.ReadBarcodes(new BarcodeReadOptions { Preprocessing = [BarcodePreprocessing.None, BarcodePreprocessing.Enlarge] });
        Assert.IsTrue(read.Found);
        Assert.AreEqual(BarcodePreprocessing.Enlarge, read.Preprocessing);
        // Each side scales by what it was rounded to, so the two can differ a little.
        var longer = Math.Max(image.Width, image.Height);
        Assert.AreEqual(Math.Round(image.Width * 1200.0 / longer) / image.Width, read.ScaleX, 1e-12);
        Assert.AreEqual(Math.Round(image.Height * 1200.0 / longer) / image.Height, read.ScaleY, 1e-12);
        Assert.IsGreaterThan(4, read.ScaleY);
        var found = read.Barcodes[0].Corners[0];
        var mapped = read.CornersOf(read.Barcodes[0])[0];
        Assert.AreEqual(found.X / read.ScaleX, mapped.X, 1e-3);
        Assert.AreEqual(found.Y / read.ScaleY, mapped.Y, 1e-3);
        // The code sits in the middle third across and half down; its corners, mapped back, lie there in the original.
        foreach (var corner in read.CornersOf(read.Barcodes[0]))
        {
            Assert.IsTrue(corner.X > image.Width / 3f - 4 && corner.X < image.Width * 2 / 3f + 4, $"{corner}");
            Assert.IsTrue(corner.Y > image.Height / 4f - 4 && corner.Y < image.Height * 3 / 4f + 4, $"{corner}");
        }
    }

    [TestMethod]
    public void ASoftLinearCodeReadsSharpened()
    {
        using var image = Code128(scale: 2, blur: 1.6);
        Assert.IsEmpty(image.Decode());
        var read = image.ReadBarcodes();
        Assert.AreEqual(BarcodePreprocessing.Sharpen, read.Preprocessing);
        Assert.AreEqual("BMAGE-1234567", read.Barcodes[0].Text);
        Assert.AreEqual(BbBarcodeFormat.Code128, read.Barcodes[0].Format);
    }

    [TestMethod]
    public void AnImageWithoutACodeReportsNothingFound()
    {
        using var blank = ImageBuffer.Create(300, 200, Rgba32.White);
        var read = blank.ReadBarcodes();
        Assert.IsFalse(read.Found);
        Assert.IsNull(read.Preprocessing);
        // Enlarging is skipped for an image already as large as asked.
        var large = blank.ReadBarcodes(new BarcodeReadOptions { Preprocessing = [BarcodePreprocessing.Enlarge], MinimumSize = 300 });
        Assert.IsFalse(large.Found);
    }

    [TestMethod]
    public void OptionsAreChecked()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BarcodeReadOptions { MaxSymbols = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BarcodeReadOptions { MinimumSize = 10 });
        using var image = ImageBuffer.Create(10, 10);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.Decode(maxSymbols: 0));
        Assert.ThrowsExactly<ArgumentNullException>(() => BmageBarcodes.Read(null!));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => image.ReadBarcodes(cancellationToken: cts.Token));
    }

    internal static int MaxDifference(ImageBuffer a, ImageBuffer b)
    {
        var max = 0;
        var x = a.Bytes;
        var y = b.Bytes;
        for (var i = 0; i < x.Length; i++) max = Math.Max(max, Math.Abs(x[i] - y[i]));
        return max;
    }
}
