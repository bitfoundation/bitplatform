namespace Bit.BbScanner.Tests;

/// <summary>
/// Frames whose rows are further apart than their pixels: bitmaps padded to a row alignment, and windows into larger
/// images, decoded in place through the stride overloads of <see cref="BbScannerBarcodeDecoder"/>.
/// </summary>
[TestClass]
public class StridedFrameTests
{
    /// <summary>Spreads a grayscale image into <paramref name="format"/>, each row <paramref name="padding"/> bytes longer than its pixels, the padding filled with noise.</summary>
    private static byte[] Spread(BbScannerLuminanceBuffer image, BbScannerPixelFormat format, int padding, out int stride)
    {
        var bpp = BbScannerPixelConverter.BytesPerPixel(format);
        stride = (image.Width * bpp) + padding;
        var pixels = new byte[stride * image.Height];
        var random = new Random(7);
        random.NextBytes(pixels);

        var view = image.View;
        for (var y = 0; y < image.Height; y++)
        {
            var row = view.GetRow(y);
            for (var x = 0; x < image.Width; x++)
            {
                var at = (y * stride) + (x * bpp);
                for (var channel = 0; channel < Math.Min(bpp, 3); channel++)
                {
                    pixels[at + channel] = row[x];
                }

                if (bpp == 4)
                {
                    pixels[at + 3] = 255;
                }
            }
        }

        return pixels;
    }

    [TestMethod]
    [DataRow(BbScannerPixelFormat.Rgb24, 1, DisplayName = "A 24 bit bitmap padded to four bytes")]
    [DataRow(BbScannerPixelFormat.Rgba32, 64, DisplayName = "A 32 bit bitmap padded to a cache line")]
    [DataRow(BbScannerPixelFormat.Bgra32, 4, DisplayName = "A BGRA surface")]
    [DataRow(BbScannerPixelFormat.Gray8, 13, DisplayName = "A camera's luminance plane")]
    public void AFrameWithPaddedRowsReadsAsItsPackedCopyDoes(BbScannerPixelFormat format, int padding)
    {
        using var image = SyntheticImage.FromMatrix(QrEncoder.Encode("STRIDED ROWS"), scale: 4);
        var pixels = Spread(image, format, padding, out var stride);
        using var decoder = new BbScannerBarcodeDecoder();

        var result = decoder.Decode(pixels, image.Width, image.Height, stride, format, suppressDuplicates: false);
        var packed = decoder.Decode(Spread(image, format, 0, out _), image.Width, image.Height, format, suppressDuplicates: false);

        Assert.AreEqual("STRIDED ROWS", result?.Text);
        CollectionAssert.AreEqual(packed!.Corners.ToArray(), result!.Corners.ToArray());
    }

    [TestMethod]
    public void AWindowOfALargerImageIsReadWithoutCopyingItOut()
    {
        // The symbol is the right half of a canvas twice its width: the window starts at the half and steps by the
        // canvas's rows.
        using var image = SyntheticImage.FromMatrix(QrEncoder.Encode("WINDOW"), scale: 4);
        var symbol = Spread(image, BbScannerPixelFormat.Rgba32, 0, out var rowLength);
        var stride = rowLength * 2;
        var canvas = new byte[stride * image.Height];
        new Random(3).NextBytes(canvas);
        for (var y = 0; y < image.Height; y++)
        {
            symbol.AsSpan(y * rowLength, rowLength).CopyTo(canvas.AsSpan((y * stride) + rowLength));
        }

        var window = canvas.AsSpan(rowLength);
        using var decoder = new BbScannerBarcodeDecoder();

        // Without its last row's padding the window is shorter than stride times height; it still holds every pixel.
        var result = decoder.Decode(window[..((stride * (image.Height - 1)) + (image.Width * 4))], image.Width, image.Height, stride, BbScannerPixelFormat.Rgba32);

        Assert.AreEqual("WINDOW", result?.Text);
    }

    [TestMethod]
    public void EverySymbolOfAPaddedFrameIsFound()
    {
        using var first = SyntheticImage.FromMatrix(QrEncoder.Encode("LEFT"), scale: 4);
        using var image = ImageTransforms.Frame(first, first.Width * 2, first.Height);
        var pixels = Spread(image, BbScannerPixelFormat.Rgb24, 3, out var stride);
        using var decoder = new BbScannerBarcodeDecoder();

        var results = decoder.DecodeAll(pixels, image.Width, image.Height, stride, BbScannerPixelFormat.Rgb24, maxSymbols: 4);

        Assert.AreEqual("LEFT", results.Single().Text);
    }

    [TestMethod]
    public void ARegionIsCutFromAPaddedFrame()
    {
        using var image = SyntheticImage.FromMatrix(QrEncoder.Encode("REGION"), scale: 4);
        using var framed = ImageTransforms.Frame(image, image.Width * 3, image.Height * 3);
        var pixels = Spread(framed, BbScannerPixelFormat.Rgb24, 5, out var stride);
        using var decoder = new BbScannerBarcodeDecoder(new BbScannerOptions { Region = BbScannerScanRegion.CenteredSquare(0.6) });

        var result = decoder.Decode(pixels, framed.Width, framed.Height, stride, BbScannerPixelFormat.Rgb24);
        using var packedDecoder = new BbScannerBarcodeDecoder(new BbScannerOptions { Region = BbScannerScanRegion.CenteredSquare(0.6) });
        var packed = packedDecoder.Decode(Spread(framed, BbScannerPixelFormat.Rgb24, 0, out _), framed.Width, framed.Height, BbScannerPixelFormat.Rgb24);

        Assert.AreEqual("REGION", result?.Text);
        CollectionAssert.AreEqual(packed!.Corners.ToArray(), result!.Corners.ToArray(), "Corners are in the frame's coordinates, whatever its stride.");
    }

    [TestMethod]
    public void AStrideNarrowerThanARowOrPixelsShortOfTheLastRowAreRejected()
    {
        using var decoder = new BbScannerBarcodeDecoder();
        var pixels = new byte[304 * 50];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => decoder.Decode(pixels, 100, 50, (100 * 3) - 1, BbScannerPixelFormat.Rgb24));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => decoder.Decode(pixels, 100, 50, -300, BbScannerPixelFormat.Rgb24));
        Assert.ThrowsExactly<ArgumentException>(() => decoder.Decode(pixels.AsSpan(0, (304 * 49) + 299), 100, 50, 304, BbScannerPixelFormat.Rgb24));
        Assert.ThrowsExactly<ArgumentException>(() => decoder.DecodeAll(pixels.AsSpan(0, (300 * 49) + 299), 100, 50, 300, BbScannerPixelFormat.Rgb24));

        // Exactly enough: every row but the last at its stride, the last one only as long as its pixels.
        Assert.IsNull(decoder.Decode(pixels.AsSpan(0, (304 * 49) + 300), 100, 50, 304, BbScannerPixelFormat.Rgb24));
    }

    [TestMethod]
    public void AStrideOfZeroMeansPackedRows()
    {
        using var image = SyntheticImage.FromMatrix(QrEncoder.Encode("PACKED"), scale: 4);
        var pixels = Spread(image, BbScannerPixelFormat.Rgba32, 0, out _);
        using var decoder = new BbScannerBarcodeDecoder();

        Assert.AreEqual("PACKED", decoder.Decode(pixels, image.Width, image.Height, 0, BbScannerPixelFormat.Rgba32)?.Text);
    }
}
