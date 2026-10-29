using Bit.Bmage.Capabilities;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Codecs.WebP;
using Bit.Bmage.Geometry;
using Bit.Bmage.Metadata;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class WebPCodecTests
{
    // Assets/WebP holds files written by libwebp (through ImageMagick), each next to the PNG libwebp decodes it to.
    private static byte[] Asset(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "WebP", name));

    [TestMethod]
    [DataRow("lossy")]
    [DataRow("lossy-alpha")]
    [DataRow("animated")]
    public void DecodesLossyFilesAsLibwebpDoes(string name)
    {
        using var expected = PngDecoder.Decode(Asset(name + ".png"));
        using var decoded = WebPDecoder.Decode(Asset(name + ".webp"));
        Assert.AreEqual(expected.Width, decoded.Width);
        Assert.AreEqual(expected.Height, decoded.Height);
        // The reconstruction is bit exact; only the final YUV to RGB rounding may differ by one.
        Assert.IsLessThanOrEqualTo(1, TestImages.MaxChannelDifference(expected, decoded));
    }

    [TestMethod]
    [DataRow("lossless")]
    [DataRow("lossless-palette")]
    public void DecodesLosslessFilesExactly(string name)
    {
        using var expected = PngDecoder.Decode(Asset(name + ".png"));
        using var decoded = WebPDecoder.Decode(Asset(name + ".webp"));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, decoded));
    }

    [TestMethod]
    public void IdentifyReadsTheHeaderOfEveryKind()
    {
        var lossy = new WebPDecoder().Identify(Asset("lossy.webp"))!;
        Assert.AreEqual((93, 62, false), (lossy.Width, lossy.Height, lossy.HasAlpha));
        var alpha = new WebPDecoder().Identify(Asset("lossy-alpha.webp"))!;
        Assert.IsTrue(alpha.HasAlpha);
        var lossless = new WebPDecoder().Identify(Asset("lossless.webp"))!;
        Assert.AreEqual(ImageFormat.WebP, lossless.Format);
        Assert.AreEqual(93, lossless.Width);
        Assert.IsNull(new WebPDecoder().Identify(Asset("lossy.png")));
    }

    [TestMethod]
    [DataRow(0.3)]
    [DataRow(0.75)]
    [DataRow(1.0)]
    public void LossyRoundTripIsCloseToTheSource(double quality)
    {
        using var source = TestImages.Gradient(77, 45);
        var encoded = WebPEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.WebP, Quality = quality });
        Assert.AreEqual(ImageFormat.WebP, ImageFormats.Detect(encoded.Data));
        Assert.AreEqual(quality, encoded.Quality);
        using var decoded = WebPDecoder.Decode(encoded.Data);
        Assert.AreEqual((77, 45), (decoded.Width, decoded.Height));
        Assert.IsLessThan(quality < 0.5 ? 6.0 : 3.0, TestImages.MeanChannelDifference(source, decoded));
    }

    [TestMethod]
    public void LowerQualityMakesSmallerFiles()
    {
        using var source = PngDecoder.Decode(Asset("lossy.png"));
        var sizes = new[] { 0.2, 0.6, 0.95 }.Select(q => WebPEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.WebP, Quality = q }).Length).ToArray();
        Assert.IsLessThan(sizes[1], sizes[0]);
        Assert.IsLessThan(sizes[2], sizes[1]);
    }

    [TestMethod]
    public void LossyKeepsAlphaExactly()
    {
        using var source = TestImages.Translucent(29, 19);
        var encoded = WebPEncoder.Encode(source, ImageExportOptions.WebP);
        Assert.IsTrue(new WebPDecoder().Identify(encoded.Data)!.HasAlpha);
        using var decoded = WebPDecoder.Decode(encoded.Data);
        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
                Assert.AreEqual(source[x, y].A, decoded[x, y].A);
    }

    [TestMethod]
    public void OpaqueLossyImagesUseTheSimpleFormat()
    {
        using var source = TestImages.Gradient(16, 16);
        var encoded = WebPEncoder.Encode(source, ImageExportOptions.WebP);
        CollectionAssert.AreEqual("VP8 "u8.ToArray(), encoded.Data[12..16]);
        Assert.IsFalse(new WebPDecoder().Identify(encoded.Data)!.HasAlpha);
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(3, 1)]
    [DataRow(1, 7)]
    [DataRow(31, 17)]
    [DataRow(130, 66)]
    public void LosslessRoundTripsExactly(int width, int height)
    {
        using var source = TestImages.Translucent(width, height);
        var encoded = WebPEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.WebP, Lossless = true });
        CollectionAssert.AreEqual("VP8L"u8.ToArray(), encoded.Data[12..16]);
        Assert.IsNull(encoded.Quality);
        using var decoded = WebPDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded));
    }

    [TestMethod]
    public void LosslessRoundTripsPhotosAndPalettes()
    {
        foreach (var name in new[] { "lossy.png", "lossless-palette.png" })
        {
            using var source = PngDecoder.Decode(Asset(name));
            var encoded = WebPEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.WebP, Lossless = true });
            using var decoded = WebPDecoder.Decode(encoded.Data);
            Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded), name);
        }
    }

    [TestMethod]
    public void TinyAndOddSizedLossyImagesRoundTrip()
    {
        foreach (var (w, h) in new[] { (1, 1), (2, 1), (1, 3), (17, 33), (16, 16) })
        {
            using var source = TestImages.Translucent(w, h);
            var encoded = WebPEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.9 });
            using var decoded = WebPDecoder.Decode(encoded.Data);
            Assert.AreEqual((w, h), (decoded.Width, decoded.Height));
        }
    }

    [TestMethod]
    public void FullyTransparentImagesStayTransparent()
    {
        using var source = ImageBuffer.Create(40, 24, Rgba32.Transparent);
        var encoded = WebPEncoder.Encode(source, ImageExportOptions.WebP);
        using var decoded = WebPDecoder.Decode(encoded.Data);
        Assert.IsTrue(decoded.Pixels.ToArray().All(p => p.A == 0));
    }

    [TestMethod]
    public void PreservesMetadataWhenAsked()
    {
        using var source = TestImages.Translucent(12, 12);
        var exif = new ExifData().WithPrimary(new ExifDirectory().With(ExifTag.Make, ExifValue.FromString("Bmage")));
        source.Metadata = new ImageMetadata(exif, iccProfile: new byte[] { 1, 2, 3, 4 }, xmp: "<x:xmpmeta/>"u8.ToArray());
        var encoded = WebPEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.WebP, Metadata = MetadataPolicy.Preserve });
        using var decoded = WebPDecoder.Decode(encoded.Data);
        Assert.AreEqual("Bmage", decoded.Metadata?.Exif?.Make);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, decoded.Metadata!.IccProfile.ToArray());
        Assert.IsFalse(decoded.Metadata.Xmp.IsEmpty);
        // Alpha survives the extended header the metadata needs.
        Assert.IsTrue(new WebPDecoder().Identify(encoded.Data)!.HasAlpha);

        var stripped = WebPEncoder.Encode(source, ImageExportOptions.WebP);
        using var plain = WebPDecoder.Decode(stripped.Data);
        Assert.IsNull(plain.Metadata?.Exif);
    }

    [TestMethod]
    public void AppliesExifOrientationOnDecode()
    {
        using var source = TestImages.Quadrants(8, 8);
        var encoded = WebPEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.WebP, Lossless = true });
        var rotated = WebPContainer.WriteMetadata(encoded.Data, new ImageMetadata(ExifData.FromOrientation(Orientation.Rotate90)));
        Assert.AreEqual(Orientation.Rotate90, new WebPDecoder().Identify(rotated)!.Orientation);
        using var decoded = WebPDecoder.Decode(rotated, new DecodeOptions { AutoOrient = true });
        Assert.AreEqual(Orientation.Normal, decoded.Metadata!.Orientation);
        Assert.AreEqual(new Rgba32(0, 0, 255), decoded[0, 0]);
    }

    [TestMethod]
    public void RefusesImagesWebPCannotHold()
    {
        using var wide = ImageBuffer.Create(WebPEncoder.MaxDimension + 1, 1, limits: ImageLimits.Unrestricted);
        Assert.ThrowsExactly<ImageEncodeException>(() => WebPEncoder.Encode(wide, ImageExportOptions.WebP));
    }

    [TestMethod]
    public async Task TheHeadlessServicesReadAndWriteWebP()
    {
        Assert.IsTrue(ImageCapabilities.Managed.CanDecode(ImageFormat.WebP));
        Assert.IsTrue(ImageCapabilities.Managed.CanEncode(ImageFormat.WebP));
        var processor = new ImageProcessor(ImageCodecRegistry.CreateDefault());
        using var source = TestImages.Gradient(40, 30);
        var encoded = await processor.EncodeAsync(source, new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.8 });
        using var decoded = await processor.DecodeAsync(encoded.Data);
        Assert.AreEqual((40, 30), (decoded.Width, decoded.Height));
        Assert.AreEqual(ImageFormat.WebP, processor.Identify(encoded.Data)!.Format);
    }

    [TestMethod]
    public async Task ASizeBudgetIsMetByLoweringQuality()
    {
        var processor = new ImageProcessor(ImageCodecRegistry.CreateDefault());
        using var source = PngDecoder.Decode(Asset("lossy.png"));
        var full = await processor.EncodeAsync(source, new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.95 });
        var budget = full.Length * 2 / 3;
        var fitted = await processor.EncodeAsync(source, new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.95, MaxFileSize = budget, MinQuality = 0.05 });
        Assert.IsLessThanOrEqualTo(budget, fitted.Length);
        Assert.IsLessThan(0.95, fitted.Quality!.Value);
        Assert.AreEqual((source.Width, source.Height), (fitted.Width, fitted.Height));
    }

    // ------------------------------------------------------------------ untrusted input

    [TestMethod]
    public void RejectsTruncatedFiles()
    {
        foreach (var name in new[] { "lossy", "lossy-alpha", "lossless", "lossless-palette" })
        {
            var data = Asset(name + ".webp");
            foreach (var length in new[] { 12, 20, 30, data.Length / 2, data.Length - 3 })
                Assert.ThrowsExactly<ImageDecodeException>(() => WebPDecoder.Decode(data[..length]), $"{name} cut to {length}");
        }
        // An animation cut short anywhere before the end of its first frame.
        var animated = Asset("animated.webp");
        foreach (var length in new[] { 12, 30, 60, 120 })
            Assert.ThrowsExactly<ImageDecodeException>(() => WebPDecoder.Decode(animated[..length]), $"animated cut to {length}");
    }

    [TestMethod]
    public void RejectsCanvasesOverTheLimitsBeforeAllocating()
    {
        // A tiny extended header claiming a 16 million by 16 million canvas.
        var data = WebPContainer.WriteMetadata(Asset("lossy.webp"), new ImageMetadata(xmp: "<x/>"u8.ToArray()));
        var vp8x = data.AsSpan(20, 10);
        vp8x[4] = vp8x[5] = vp8x[6] = vp8x[7] = vp8x[8] = vp8x[9] = 0xFF;
        Assert.ThrowsExactly<ImageLimitExceededException>(() => WebPDecoder.Decode(data));
        var limits = new DecodeOptions { Limits = new ImageLimits { MaxWidth = 50 } };
        Assert.ThrowsExactly<ImageLimitExceededException>(() => WebPDecoder.Decode(Asset("lossless.webp"), limits));
    }

    [TestMethod]
    public void RejectsAnImageThatDisagreesWithItsCanvas()
    {
        var data = WebPContainer.WriteMetadata(Asset("lossy.webp"), new ImageMetadata(xmp: "<x/>"u8.ToArray()));
        data[24]++; // canvas one pixel wider than the frame
        Assert.ThrowsExactly<ImageDecodeException>(() => WebPDecoder.Decode(data));
    }

    [TestMethod]
    public void CorruptFilesFailOnlyWithDecodeErrors()
    {
        var random = new Random(14333);
        var seeds = new[] { "lossy", "lossy-alpha", "lossless", "lossless-palette", "animated" }.Select(n => Asset(n + ".webp")).ToArray();
        for (var i = 0; i < 1500; i++)
        {
            var data = (byte[])seeds[i % seeds.Length].Clone();
            for (var k = random.Next(1, 6); k > 0; k--)
            {
                var at = random.Next(12, data.Length);
                if (random.Next(2) == 0) data[at] ^= (byte)(1 << random.Next(8));
                else data[at] = (byte)random.Next(256);
            }
            try
            {
                using var image = WebPDecoder.Decode(data);
            }
            catch (Exception ex) when (ex is ImageDecodeException or ImageLimitExceededException)
            {
                // Rejected cleanly.
            }
        }
    }
}
