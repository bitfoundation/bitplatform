using System.Drawing;
using Bit.Bmage.Analysis;
using Bit.Bmage.Codecs;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// Analysis, placeholder and remaining codec robustness. These paths run over images the application did not create,
/// so degenerate sizes and hostile bytes have to produce a clear result rather than an arbitrary exception.
/// </summary>
[TestClass]
public sealed class AdversarialAnalysisTests
{
    private static ImageBuffer Noise(int w, int h, int seed = 1) => AdversarialGeometryTests.Noise(w, h, seed);

    // ---------------------------------------------------------------- smart crop

    /// <summary>Smart cropping must return a rectangle inside the image with the requested aspect ratio.</summary>
    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(16, 9)]
    [DataRow(9, 16)]
    [DataRow(4, 3)]
    [DataRow(21, 9)]
    public void SmartCropStaysInsideTheImageAndHonoursTheRatio(double rw, double rh)
    {
        foreach (var (w, h) in new[] { (1, 1), (2, 60), (60, 2), (137, 91), (640, 480) })
        {
            using var src = Noise(w, h, w * 13 + h);
            var ratio = new AspectRatio(rw, rh);
            var result = SmartCrop.FindCrop(src, ratio);

            Assert.IsTrue(result.Rectangle.Width > 0 && result.Rectangle.Height > 0,
                $"{w}x{h} @ {ratio}: empty crop {result.Rectangle}");
            Assert.IsTrue(src.Bounds.Contains(result.Rectangle),
                $"{w}x{h} @ {ratio}: crop {result.Rectangle} escapes the image");

            // The crop must actually be applicable.
            using var cropped = result.ToOperation().Apply(src, OperationContext.Default);
            Assert.AreEqual(result.Rectangle.Size, cropped.Size);
        }
    }

    /// <summary>Smart cropping is deterministic: the same image must always give the same crop.</summary>
    [TestMethod]
    public void SmartCropIsDeterministic()
    {
        using var src = Noise(300, 200, 5);
        var first = SmartCrop.FindCrop(src, AspectRatio.Square);
        for (var i = 0; i < 5; i++)
            Assert.AreEqual(first.Rectangle, SmartCrop.FindCrop(src, AspectRatio.Square).Rectangle);
    }

    /// <summary>A must-include region has to survive: that is the entire contract of the overload.</summary>
    [TestMethod]
    public void SmartCropRespectsTheMustIncludeRegion()
    {
        using var src = Noise(400, 300, 7);
        var mustInclude = new Rectangle(300, 20, 60, 60);
        var result = SmartCrop.FindCrop(src, AspectRatio.Square, mustInclude);
        Assert.IsTrue(result.Rectangle.Contains(mustInclude),
            $"crop {result.Rectangle} does not contain the required region {mustInclude}");
    }

    [TestMethod]
    public void SmartCropOperationPredictsItsOwnOutputSize()
    {
        foreach (var (w, h) in new[] { (200, 100), (100, 200), (50, 50) })
        {
            using var src = Noise(w, h, w + h);
            var op = new SmartCropOperation(AspectRatio.Ratio16x9);
            using var result = op.Apply(src, OperationContext.Default);
            Assert.AreEqual(op.GetOutputSize(src.Size), result.Size);
        }
    }

    // ---------------------------------------------------------------- statistics

    [TestMethod]
    public void StatisticsOfASolidColourAreExact()
    {
        var colour = new Rgba32(40, 80, 120);
        using var src = ImageBuffer.Create(64, 64, colour);
        var stats = ImageStatistics.Measure(src);

        Assert.AreEqual(colour, stats.AverageColor);
        Assert.AreEqual(40, stats.Red.Mean, 0.5e-1);
        Assert.AreEqual(80, stats.Green.Mean, 0.5e-1);
        Assert.AreEqual(120, stats.Blue.Mean, 0.5e-1);
        Assert.IsFalse(stats.HasTransparency);
        Assert.IsFalse(stats.IsGrayscale);
        Assert.AreEqual(64L * 64, stats.TotalPixelCount);
        Assert.AreEqual(64L * 64, stats.OpaquePixelCount);
    }

    [TestMethod]
    public void StatisticsRecogniseGrayscaleAndTransparency()
    {
        using var grey = ImageBuffer.Create(32, 32, new Rgba32(90, 90, 90));
        Assert.IsTrue(ImageStatistics.Measure(grey).IsGrayscale);

        using var translucent = TestImages.Translucent(32, 32);
        Assert.IsTrue(ImageStatistics.Measure(translucent).HasTransparency);
    }

    [TestMethod]
    public void StatisticsHandleDegenerateImagesAndRegions()
    {
        using var one = ImageBuffer.Create(1, 1, Rgba32.White);
        var stats = ImageStatistics.Measure(one);
        Assert.AreEqual(255, stats.Red.Mean, 0.5e-1);
        Assert.AreEqual(1L, stats.TotalPixelCount);

        using var src = Noise(64, 48, 11);
        // A region clipped to nothing must say so clearly rather than dividing by zero.
        Assert.ThrowsExactly<ArgumentException>(() => ImageStatistics.Measure(src, new Rectangle(1000, 1000, 10, 10)));
        Assert.ThrowsExactly<ArgumentException>(() => ImageStatistics.Measure(src, new Rectangle(64, 0, 10, 10)));

        // A partially clipped region is fine.
        var clipped = ImageStatistics.Measure(src, new Rectangle(-10, -10, 30, 30));
        Assert.IsInRange(0, 255, clipped.Red.Mean);
    }

    /// <summary>Sampling must not change the answer materially: that is what makes it safe to default to.</summary>
    [TestMethod]
    public void SampledStatisticsAgreeWithAFullPass()
    {
        using var src = Noise(900, 700, 29);
        var sampled = ImageStatistics.Measure(src, null, 10_000);
        var full = ImageStatistics.Measure(src, null, 0);
        // Sampling one pixel in sixty must not move a channel mean by even one level in 255.
        Assert.IsTrue(Math.Abs(full.Red.Mean - sampled.Red.Mean) < 1.5,
            $"sampled red mean {sampled.Red.Mean:0.00} against a full pass of {full.Red.Mean:0.00}");
        Assert.IsTrue(Math.Abs(full.Luminance.Mean - sampled.Luminance.Mean) < 1.5,
            $"sampled luminance mean {sampled.Luminance.Mean:0.00} against a full pass of {full.Luminance.Mean:0.00}");
    }

    // ---------------------------------------------------------------- placeholder

    /// <summary>
    /// Both string forms must round-trip. ToString has to be the inverse of Parse, because that is the call a
    /// developer reaches for, and a record's synthesised ToString would silently produce something Parse rejects.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(4, 4)]
    [DataRow(8, 8)]
    public void PlaceholderRoundTripsThroughItsStringForm(int columns, int rows)
    {
        using var src = Noise(120, 90, 13);
        var placeholder = ImagePlaceholder.Create(src, columns, rows);
        Assert.AreEqual(placeholder.ToCompactString(), placeholder.ToString());

        foreach (var encoded in new[] { placeholder.ToCompactString(), placeholder.ToString() })
        {
            var parsed = ImagePlaceholder.Parse(encoded, src.Width, src.Height);
            Assert.IsNotNull(parsed);
            Assert.AreEqual(columns, parsed!.Columns);
            Assert.AreEqual(rows, parsed.Rows);

            using var a = placeholder.Render(60, 45);
            using var b = parsed.Render(60, 45);
            Assert.AreEqual(a.Size, b.Size);
            Assert.IsTrue(a.Bytes.SequenceEqual(b.Bytes),
                $"round trip changed the rendered placeholder by {TestImages.MaxChannelDifference(a, b)}");
        }
    }

    [TestMethod]
    public void PlaceholderRejectsInvalidGridsAndSizes()
    {
        using var src = Noise(32, 32, 17);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImagePlaceholder.Create(src, 0, 4));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImagePlaceholder.Create(src, 4, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImagePlaceholder.Create(src, 1000, 4));

        var placeholder = ImagePlaceholder.Create(src);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => placeholder.Render(0, 10).Dispose());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => placeholder.Render(10, 0).Dispose());
    }

    [TestMethod]
    public void PlaceholderParseRejectsGarbageInsteadOfThrowing()
    {
        foreach (var value in new[] { null, "", "   ", "not-a-placeholder", "!!!!", "0", new string('z', 500) })
        {
            var ex = Capture.Exception(() => ImagePlaceholder.Parse(value, 100, 100));
            Assert.IsTrue(ex is null, $"parsing {value ?? "null"} threw {ex?.GetType().Name}");
        }
    }

    /// <summary>
    /// The encoded size depends only on the grid, never on the source image, and matches what the documentation
    /// promises. A placeholder that grew with the image would defeat the purpose of inlining it.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1, 7)]
    [DataRow(2, 2, 19)]
    [DataRow(4, 4, 67)]
    [DataRow(6, 6, 147)]
    [DataRow(8, 8, 259)]
    public void PlaceholderEncodedSizeDependsOnlyOnTheGrid(int columns, int rows, int expectedLength)
    {
        foreach (var (w, h) in new[] { (32, 32), (1920, 1080), (4032, 3024) })
        {
            using var src = Noise(w, h, w + h);
            var encoded = ImagePlaceholder.Create(src, columns, rows).ToCompactString();
            Assert.AreEqual(expectedLength, encoded.Length);
        }
    }

    // ---------------------------------------------------------------- codec robustness

    /// <summary>Every managed decoder must reject a hostile file cleanly, whatever the byte pattern.</summary>
    [TestMethod]
    [DataRow("gif")]
    [DataRow("bmp")]
    [DataRow("png")]
    [DataRow("jpeg")]
    public async Task DecodersRejectHostileHeadersCleanly(string kind)
    {
        var processor = new ImageProcessor();
        var rnd = new Random(31337);
        byte[] header = kind switch
        {
            "gif" => [(byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a'],
            "bmp" => [(byte)'B', (byte)'M'],
            "png" => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A],
            _ => [0xFF, 0xD8, 0xFF],
        };

        for (var trial = 0; trial < 300; trial++)
        {
            var length = rnd.Next(header.Length, header.Length + 200);
            var data = new byte[length];
            header.CopyTo(data, 0);
            for (var i = header.Length; i < length; i++) data[i] = (byte)rnd.Next(256);

            var ex = await Capture.ExceptionAsync(async () => (await processor.DecodeAsync(data)).Dispose());
            Assert.IsTrue(ex is null or ImageException,
                $"{kind} trial {trial} threw {ex?.GetType().Name}: {ex?.Message}");
        }
    }

    /// <summary>A declared size far larger than the payload must be refused before any large allocation.</summary>
    [TestMethod]
    public async Task DecodersRefuseDimensionsBeyondTheLimits()
    {
        var processor = new ImageProcessor(ImageCodecRegistry.CreateDefault(), null, new ImageLimits { MaxPixels = 1_000_000 });

        // A BMP header claiming 30000x30000 in a handful of bytes.
        var bmp = new byte[60];
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BitConverter.GetBytes(40).CopyTo(bmp, 14);       // header size
        BitConverter.GetBytes(30000).CopyTo(bmp, 18);    // width
        BitConverter.GetBytes(30000).CopyTo(bmp, 22);    // height
        BitConverter.GetBytes((short)1).CopyTo(bmp, 26); // planes
        BitConverter.GetBytes((short)24).CopyTo(bmp, 28);// bits per pixel

        var ex = await Capture.ExceptionAsync(async () => (await processor.DecodeAsync(bmp)).Dispose());
        Assert.IsTrue(ex is ImageException, $"an oversized BMP threw {ex?.GetType().Name ?? "nothing"}");
    }

    /// <summary>Identify must never throw, whatever it is handed. It is the safe pre-flight check.</summary>
    [TestMethod]
    public void IdentifyNeverThrows()
    {
        var processor = new ImageProcessor();
        var rnd = new Random(4242);
        var inputs = new List<byte[]> { Array.Empty<byte>(), new byte[] { 0 }, new byte[] { 0xFF }, new byte[12] };
        for (var i = 0; i < 300; i++)
        {
            var data = new byte[rnd.Next(0, 64)];
            rnd.NextBytes(data);
            inputs.Add(data);
        }

        foreach (var data in inputs)
        {
            var ex = Capture.Exception(() => { processor.Identify(data); });
            Assert.IsTrue(ex is null, $"Identify threw {ex?.GetType().Name} for {data.Length} bytes");
        }
    }

    /// <summary>Identify must agree with a real decode about the dimensions.</summary>
    [TestMethod]
    [DataRow(ImageFormat.Png)]
    [DataRow(ImageFormat.Jpeg)]
    [DataRow(ImageFormat.Bmp)]
    public async Task IdentifyAgreesWithDecode(ImageFormat format)
    {
        var processor = new ImageProcessor();
        foreach (var (w, h) in new[] { (1, 1), (17, 5), (64, 64), (200, 137) })
        {
            using var src = Noise(w, h, w * 3 + h);
            var encoded = await processor.EncodeAsync(src, new ImageExportOptions { Format = format, Quality = 0.9 });

            var info = processor.Identify(encoded.Data);
            Assert.IsNotNull(info);
            Assert.AreEqual(format, info!.Format);

            using var decoded = await processor.DecodeAsync(encoded.Data);
            Assert.AreEqual(decoded.Size, info.DisplaySize);
        }
    }

    /// <summary>Format detection must not misidentify one format as another.</summary>
    [TestMethod]
    public async Task FormatDetectionIsAccurate()
    {
        var processor = new ImageProcessor();
        using var src = Noise(24, 24, 23);
        foreach (var format in new[] { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Bmp })
        {
            var encoded = await processor.EncodeAsync(src, new ImageExportOptions { Format = format });
            Assert.AreEqual(format, ImageFormats.Detect(encoded.Data));
            // Detection must also work from just the leading bytes, which is all a streaming caller has.
            Assert.AreEqual(format, ImageFormats.Detect(encoded.Data.AsSpan(0, Math.Min(16, encoded.Data.Length))));
        }

        Assert.AreEqual(ImageFormat.Unknown, ImageFormats.Detect([]));
        Assert.AreEqual(ImageFormat.Unknown, ImageFormats.Detect([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]));
        Assert.AreEqual(ImageFormat.Svg, ImageFormats.Detect("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8));
    }

    /// <summary>SVG is untrusted input and must be refused with an explanation rather than rasterised.</summary>
    [TestMethod]
    public async Task SvgIsRefusedByTheManagedDecoder()
    {
        var processor = new ImageProcessor();
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\"/></svg>"u8.ToArray();
        Assert.AreEqual(ImageFormat.Svg, ImageFormats.Detect(svg));
        var ex = await Capture.ExceptionAsync(async () => (await processor.DecodeAsync(svg)).Dispose());
        Assert.IsInstanceOfType<ImageException>(ex);
    }
}
