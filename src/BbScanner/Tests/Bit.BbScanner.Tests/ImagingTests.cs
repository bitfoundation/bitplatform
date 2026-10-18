namespace Bit.BbScanner.Tests;

/// <summary>Covers the primitives every decoding path is built on.</summary>
[TestClass]
public class ImagingTests
{
    [TestMethod]
    public void BitRowFindsRunsAcrossWordBoundaries()
    {
        var row = new BbBitRow(100);
        row[31] = true;
        row[32] = true;
        row[64] = true;

        Assert.AreEqual(31, row.GetNextSet(0));
        Assert.AreEqual(32, row.GetNextSet(32));
        Assert.AreEqual(64, row.GetNextSet(33));
        Assert.AreEqual(100, row.GetNextSet(65));
        Assert.AreEqual(0, row.GetNextUnset(0));
        Assert.AreEqual(33, row.GetNextUnset(31));
        Assert.IsTrue(row.IsRange(0, 31, false));
        Assert.IsTrue(row.IsRange(31, 33, true));
    }

    [TestMethod]
    public void ReedSolomonRejectsABlockWhoseDamageCannotBePlaced()
    {
        // Four errors on a valid five-check-codeword block, arranged so that the top three
        // syndromes vanish. The Euclidean step then never runs and the locator has degree
        // zero, which used to be reported as a clean block.
        int[] received = [18, 17, 129, 170, 206, 113, 94, 202];
        var decoder = BbReedSolomonDecoder.DataMatrix;

        Assert.IsFalse(decoder.TryDecode(received, 5, out _));
    }

    [TestMethod]
    public void ReedSolomonRejectsAMiscorrectionBeyondItsCapacity()
    {
        // A random block is almost never a codeword; whatever the algorithm makes of it, the
        // result must either be a codeword or a rejection, never a wrong "correction".
        var field = BbGenericGF.QrCodeField256;
        var decoder = BbReedSolomonDecoder.QrCode;
        var random = new Random(4242);

        for (var attempt = 0; attempt < 500; attempt++)
        {
            var block = new int[26];
            for (var i = 0; i < block.Length; i++)
            {
                block[i] = random.Next(256);
            }

            if (!decoder.TryDecode(block, 7, out _))
            {
                continue;
            }

            var poly = new BbGfPoly(field, block);
            for (var i = 0; i < 7; i++)
            {
                Assert.AreEqual(0, poly.EvaluateAt(field.Exp(i)));
            }
        }
    }

    [TestMethod]
    public void BitRowReverseIsItsOwnInverse()
    {
        var random = new Random(11);
        foreach (var size in new[] { 1, 31, 32, 33, 100, 640 })
        {
            var row = new BbBitRow(size);
            var expected = new bool[size];
            for (var i = 0; i < size; i++)
            {
                expected[i] = random.Next(2) == 0;
                row[i] = expected[i];
            }

            row.Reverse();
            for (var i = 0; i < size; i++)
            {
                Assert.AreEqual(expected[size - 1 - i], row[i]);
            }

            row.Reverse();
            for (var i = 0; i < size; i++)
            {
                Assert.AreEqual(expected[i], row[i]);
            }
        }
    }

    [TestMethod]
    public void BitRowInvertFlipsEveryBitAndNothingBeyondTheEnd()
    {
        foreach (var size in new[] { 1, 31, 32, 33, 65 })
        {
            var row = new BbBitRow(size);
            for (var i = 0; i < size; i += 2)
            {
                row[i] = true;
            }

            row.Invert();

            for (var i = 0; i < size; i++)
            {
                Assert.AreEqual(i % 2 == 1, row[i]);
            }

            // Bits past the end must stay clear, or GetNextSet would report a phantom run.
            Assert.AreEqual(size, row.GetNextSet(size));
        }
    }

    [TestMethod]
    public void BitRowResetGrowsAndClears()
    {
        var row = new BbBitRow(8);
        row[3] = true;

        row.Reset(64);
        Assert.AreEqual(64, row.Size);
        Assert.AreEqual(64, row.GetNextSet(0));
    }

    [TestMethod]
    public void LuminanceViewCropsWithoutCopying()
    {
        var data = new byte[100];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)i;
        }

        var view = new BbLuminanceView(data, 10, 10);
        var crop = view.Crop(2, 3, 4, 5);

        Assert.AreEqual(4, crop.Width);
        Assert.AreEqual(5, crop.Height);
        Assert.AreEqual(data[(3 * 10) + 2], crop[0, 0]);
        Assert.AreSame(view.Buffer, crop.Buffer);
        Assert.AreEqual(10, crop.Stride);
    }

    [TestMethod]
    public void LuminanceViewRejectsAWindowOutsideItsBuffer()
    {
        var data = new byte[16];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BbLuminanceView(data, 0, 4, 4, 5));

        var view = new BbLuminanceView(data, 4, 4);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.Crop(2, 2, 4, 4));
    }

    [TestMethod]
    [DataRow(0.0, 0.0, 1.0, 1.0, true)]
    [DataRow(0.25, 0.25, 0.5, 0.5, false)]
    public void ScanRegionReportsWhetherItCoversTheFrame(double x, double y, double w, double h, bool full)
    {
        Assert.AreEqual(full, new BbScanRegion(x, y, w, h).IsFull);
    }

    [TestMethod]
    public void ScanRegionNormalizationClampsIntoTheUnitSquare()
    {
        var region = new BbScanRegion(-0.5, 0.8, 3, 3).Normalized();

        Assert.AreEqual(0, region.X);
        Assert.AreEqual(0.8, region.Y, 1e-6);
        Assert.AreEqual(1, region.Width, 1e-6);
        Assert.AreEqual(0.2, region.Height, 1e-6);
    }

    [TestMethod]
    public void CenteredSquareRejectsAnImpossibleFraction()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BbScanRegion.CenteredSquare(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BbScanRegion.CenteredSquare(1.5));

        var region = BbScanRegion.CenteredSquare(0.5);
        Assert.AreEqual(0.25, region.X, 1e-6);
        Assert.AreEqual(0.5, region.Width, 1e-6);
        Assert.IsTrue(region.KeepSquare);
        Assert.IsFalse(region.IsFull);
    }

    [TestMethod]
    [DataRow(1280, 720)]
    [DataRow(720, 1280)]
    [DataRow(640, 640)]
    [DataRow(2560, 480)]
    public void CenteredSquareIsSquareInThePixelsOfAnyFrame(int width, int height)
    {
        var region = BbScanRegion.CenteredSquare(0.7).ForFrame(width, height);
        var side = 0.7 * Math.Min(width, height);

        Assert.AreEqual(side, region.Width * width, 1e-6);
        Assert.AreEqual(side, region.Height * height, 1e-6);
        Assert.AreEqual(0.5, region.X + (region.Width / 2), 1e-6, "The square stays centred.");
        Assert.AreEqual(0.5, region.Y + (region.Height / 2), 1e-6, "The square stays centred.");
        Assert.IsFalse(region.KeepSquare, "A region resolved for a frame is a plain rectangle.");
    }

    [TestMethod]
    public void ARegionThatIsNotSquareIgnoresTheFrameShape()
    {
        var region = new BbScanRegion(0.1, 0.2, 0.8, 0.6);

        Assert.AreEqual(region, region.ForFrame(1280, 720));
        Assert.IsTrue(BbScanRegion.CenteredSquare(1).ForFrame(500, 500).IsFull);
    }

    [TestMethod]
    public void CroppingByASquareRegionYieldsASquareWindow()
    {
        var view = new BbLuminanceView(new byte[1280 * 720], 1280, 720);
        var cropped = view.Crop(BbScanRegion.CenteredSquare(0.7));

        Assert.AreEqual(504, cropped.Width);
        Assert.AreEqual(504, cropped.Height);
    }

    [TestMethod]
    public void CroppingAViewByARegionNeverLeavesTheBuffer()
    {
        var data = new byte[64 * 48];
        var view = new BbLuminanceView(data, 64, 48);

        foreach (var region in new[]
                 {
                     new BbScanRegion(0.9, 0.9, 0.5, 0.5),
                     new BbScanRegion(0, 0, 0.001, 0.001),
                     new BbScanRegion(0.5, 0.5, 0.5, 0.5),
                 })
        {
            var crop = view.Crop(region);
            Assert.IsTrue(crop.Width >= 1 && crop.Height >= 1);
            Assert.IsTrue(crop.Offset + ((crop.Height - 1) * crop.Stride) + crop.Width <= data.Length);
        }
    }

    [TestMethod]
    [DataRow(BbPixelFormat.Rgba32, 4)]
    [DataRow(BbPixelFormat.Bgra32, 4)]
    [DataRow(BbPixelFormat.Rgb24, 3)]
    [DataRow(BbPixelFormat.Gray8, 1)]
    public void PixelConverterKnowsEveryLayout(BbPixelFormat format, int bytesPerPixel)
    {
        Assert.AreEqual(bytesPerPixel, BbPixelConverter.BytesPerPixel(format));
    }

    [TestMethod]
    public void PixelConverterProducesTheSameLuminanceFromEveryColourLayout()
    {
        const int Width = 8;
        const int Height = 4;
        var random = new Random(3);
        var rgba = new byte[Width * Height * 4];
        random.NextBytes(rgba);

        var bgra = new byte[rgba.Length];
        var rgb = new byte[Width * Height * 3];
        for (var i = 0; i < Width * Height; i++)
        {
            bgra[(i * 4) + 0] = rgba[(i * 4) + 2];
            bgra[(i * 4) + 1] = rgba[(i * 4) + 1];
            bgra[(i * 4) + 2] = rgba[(i * 4) + 0];
            rgb[(i * 3) + 0] = rgba[(i * 4) + 0];
            rgb[(i * 3) + 1] = rgba[(i * 4) + 1];
            rgb[(i * 3) + 2] = rgba[(i * 4) + 2];
        }

        var fromRgba = new byte[Width * Height];
        var fromBgra = new byte[Width * Height];
        var fromRgb = new byte[Width * Height];

        BbPixelConverter.ToGrayscale(rgba, fromRgba, Width, Height, BbPixelFormat.Rgba32);
        BbPixelConverter.ToGrayscale(bgra, fromBgra, Width, Height, BbPixelFormat.Bgra32);
        BbPixelConverter.ToGrayscale(rgb, fromRgb, Width, Height, BbPixelFormat.Rgb24);

        Assert.AreSequenceEqual(fromRgba, fromBgra);
        Assert.AreSequenceEqual(fromRgba, fromRgb);
    }

    [TestMethod]
    public void PixelConverterDownsamplesByTakingEveryNthPixel()
    {
        const int Width = 8;
        const int Height = 4;
        var gray = new byte[Width * Height];
        for (var i = 0; i < gray.Length; i++)
        {
            gray[i] = (byte)i;
        }

        var destination = new byte[(Width / 2) * (Height / 2)];
        var (w, h) = BbPixelConverter.ToGrayscaleDownsampled(gray, destination, Width, Height, BbPixelFormat.Gray8, 2);

        Assert.AreEqual(4, w);
        Assert.AreEqual(2, h);
        Assert.AreEqual(gray[0], destination[0]);
        Assert.AreEqual(gray[2], destination[1]);
        Assert.AreEqual(gray[Width * 2], destination[4]);
    }

    [TestMethod]
    public void PixelConverterRejectsBuffersThatAreTooSmall()
    {
        var source = new byte[8];
        var destination = new byte[4];

        Assert.ThrowsExactly<ArgumentException>(() =>
            BbPixelConverter.ToGrayscale(source, destination, 8, 8, BbPixelFormat.Gray8));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            BbPixelConverter.ToGrayscale(source, destination, 0, 1, BbPixelFormat.Gray8));
    }

    [TestMethod]
    public void BitMatrixInvertLeavesNoBitsSetBeyondTheWidth()
    {
        foreach (var width in new[] { 1, 31, 32, 33, 65 })
        {
            using var matrix = new BbBitMatrix(width, 3);
            matrix.Invert();

            var set = 0;
            for (var y = 0; y < matrix.Height; y++)
            {
                for (var x = 0; x < matrix.Width; x++)
                {
                    if (matrix[x, y])
                    {
                        set++;
                    }
                }
            }

            Assert.AreEqual(width * 3, set);

            // A stray bit past the width would make the row words disagree with the matrix.
            var words = matrix.GetRowWords(0);
            var trailing = width & 31;
            if (trailing != 0)
            {
                Assert.AreEqual(0u, words[^1] & ~((1u << trailing) - 1));
            }
        }
    }

    [TestMethod]
    public void BitMatrixRotationsArePureRelabelings()
    {
        var random = new Random(5);
        using var source = new BbBitMatrix(9, 6);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                source[x, y] = random.Next(2) == 0;
            }
        }

        using var rotated = source.Rotate180();
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                Assert.AreEqual(source[x, y], rotated[source.Width - 1 - x, source.Height - 1 - y]);
            }
        }

        using var quarter = source.Rotate90();
        Assert.AreEqual(source.Height, quarter.Width);
        Assert.AreEqual(source.Width, quarter.Height);
    }

    [TestMethod]
    public void DarkRegionFinderSeparatesPatchesAndIgnoresSpeckle()
    {
        using var image = new BbBitMatrix(200, 120);
        image.SetRegion(10, 10, 40, 40);
        image.SetRegion(140, 60, 40, 40);

        // Isolated pixels are sensor noise, not content, and must not join the two patches.
        var random = new Random(17);
        for (var i = 0; i < 300; i++)
        {
            image[random.Next(image.Width), random.Next(image.Height)] = true;
        }

        var regions = new BbDarkRegionFinder().Find(image, minCells: 4);

        Assert.AreEqual(2, regions.Count);
        Assert.Contains(r => r.Left <= 10 && r.Right >= 49, regions);
        Assert.Contains(r => r.Left <= 140 && r.Right >= 179, regions);
    }

    [TestMethod]
    public void DarkRegionFinderReportsNothingForABlankImage()
    {
        using var image = new BbBitMatrix(100, 100);
        Assert.IsEmpty(new BbDarkRegionFinder().Find(image, minCells: 4));
    }

    [TestMethod]
    public void GlobalHistogramBinarizerRejectsAFlatRow()
    {
        using var buffer = BbLuminanceBuffer.Rent(64, 4);
        buffer.Pixels.Fill(200);

        var row = new BbBitRow(64);
        Assert.IsFalse(new BbGlobalHistogramBinarizer().TryGetBlackRow(buffer.View, 0, row));
        Assert.IsFalse(new BbAdaptiveRowBinarizer().TryGetBlackRow(buffer.View, 0, row));
    }

    [TestMethod]
    public void AdaptiveRowBinarizerFollowsALightingGradient()
    {
        // A bar pattern under a strong gradient: the dark end of the paper is darker than the
        // light end's ink, so no single threshold can separate them.
        const int Width = 256;
        using var buffer = BbLuminanceBuffer.Rent(Width, 1);
        for (var x = 0; x < Width; x++)
        {
            var bar = (x / 8) % 2 == 0;
            var brightness = 0.25 + (0.75 * x / (Width - 1));
            buffer.Pixels[x] = (byte)((bar ? 20 : 240) * brightness);
        }

        var row = new BbBitRow(Width);
        Assert.IsTrue(new BbAdaptiveRowBinarizer().TryGetBlackRow(buffer.View, 0, row));

        var correct = 0;
        for (var x = 4; x < Width - 4; x++)
        {
            if (row[x] == ((x / 8) % 2 == 0))
            {
                correct++;
            }
        }

        Assert.IsTrue(correct > (Width - 8) * 0.95, $"Only {correct} of {Width - 8} pixels were classified correctly.");
    }

    [TestMethod]
    public void LuminanceBufferReturnsItsPooledArrayOnce()
    {
        var buffer = BbLuminanceBuffer.Rent(32, 32);
        buffer.Dispose();
        buffer.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => buffer.Array);
    }

    [TestMethod]
    public void WrappedBuffersAreNotPooled()
    {
        var data = new byte[64];
        var buffer = BbLuminanceBuffer.Wrap(data, 8, 8);

        Assert.AreSame(data, buffer.Array);
        buffer.Dispose();

        // Disposing a wrapped buffer must not return a caller owned array to the pool.
        Assert.ThrowsExactly<ObjectDisposedException>(() => buffer.Array);
    }

    [TestMethod]
    public void WrapRejectsAnUndersizedBuffer()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BbLuminanceBuffer.Wrap(new byte[10], 8, 8));
    }
}
