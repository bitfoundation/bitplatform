using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Bmp;
using Bit.Bmage.Codecs.Gif;
using Bit.Bmage.Codecs.Jpeg;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Geometry;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class PngCodecTests
{
    [TestMethod]
    public void RoundTripsOpaqueImageLosslessly()
    {
        using var source = TestImages.Gradient(37, 23);
        var encoded = PngEncoder.Encode(source);
        Assert.AreEqual(ImageFormat.Png, ImageFormats.Detect(encoded.Data));
        using var decoded = PngDecoder.Decode(encoded.Data);
        Assert.AreEqual(source.Width, decoded.Width);
        Assert.AreEqual(source.Height, decoded.Height);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded));
    }

    [TestMethod]
    public void RoundTripsAlphaLosslessly()
    {
        using var source = TestImages.Translucent(16, 16);
        var encoded = PngEncoder.Encode(source);
        using var decoded = PngDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(6)]
    [DataRow(9)]
    public void RoundTripsAtEveryCompressionLevel(int level)
    {
        using var source = TestImages.Gradient(29, 17);
        var encoded = PngEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Png, PngCompressionLevel = level });
        using var decoded = PngDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded));
    }

    [TestMethod]
    public void IdentifyReadsDimensionsWithoutDecoding()
    {
        using var source = TestImages.Translucent(21, 13);
        var encoded = PngEncoder.Encode(source);
        var info = new PngDecoder().Identify(encoded.Data);
        Assert.IsNotNull(info);
        Assert.AreEqual(21, info!.Width);
        Assert.AreEqual(13, info.Height);
        Assert.IsTrue(info.HasAlpha);
    }

    [TestMethod]
    public void OpaqueImagesEncodeWithoutAlphaChannel()
    {
        using var source = TestImages.Gradient(8, 8);
        var encoded = PngEncoder.Encode(source);
        var info = new PngDecoder().Identify(encoded.Data)!;
        Assert.IsFalse(info.HasAlpha);
    }

    [TestMethod]
    public void PreservesExifWhenPolicyIsPreserve()
    {
        using var source = TestImages.Gradient(8, 8);
        var exif = new ExifData().WithPrimary(new ExifDirectory().With(ExifTag.Make, ExifValue.FromString("Bmage")));
        source.Metadata = new ImageMetadata(exif);
        var encoded = PngEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Png, Metadata = MetadataPolicy.Preserve });
        using var decoded = PngDecoder.Decode(encoded.Data);
        Assert.AreEqual("Bmage", decoded.Metadata?.Exif?.Make);
    }

    [TestMethod]
    public void RemovesExifByDefault()
    {
        using var source = TestImages.Gradient(8, 8);
        source.Metadata = new ImageMetadata(new ExifData().WithPrimary(new ExifDirectory().With(ExifTag.Make, ExifValue.FromString("Camera"))));
        var encoded = PngEncoder.Encode(source);
        using var decoded = PngDecoder.Decode(encoded.Data);
        Assert.IsNull(decoded.Metadata?.Exif);
    }

    [TestMethod]
    public void RejectsTruncatedData()
    {
        using var source = TestImages.Gradient(32, 32);
        var encoded = PngEncoder.Encode(source);
        var truncated = encoded.Data[..(encoded.Data.Length / 2)];
        Assert.ThrowsExactly<ImageDecodeException>(() => PngDecoder.Decode(truncated));
    }

    [TestMethod]
    public void RejectsOversizedImages()
    {
        using var source = TestImages.Gradient(64, 64);
        var encoded = PngEncoder.Encode(source);
        var limits = new ImageLimits { MaxWidth = 32, MaxHeight = 32 };
        Assert.ThrowsExactly<ImageLimitExceededException>(() => PngDecoder.Decode(encoded.Data, new DecodeOptions { Limits = limits }));
    }

    [TestMethod]
    public void AppliesExifOrientationOnDecode()
    {
        using var source = TestImages.Quadrants(8, 8);
        source.Metadata = new ImageMetadata(ExifData.FromOrientation(Orientation.Rotate90));
        var encoded = PngEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Png, Metadata = MetadataPolicy.Preserve });

        // Preserve resets the stored orientation to normal, so re-inject it to simulate a camera file.
        var reOriented = PngContainer.WriteMetadata(encoded.Data, new ImageMetadata(ExifData.FromOrientation(Orientation.Rotate90)));
        using var decoded = PngDecoder.Decode(reOriented, new DecodeOptions { AutoOrient = true });
        Assert.AreEqual(Orientation.Normal, decoded.Metadata!.Orientation);
        // Top-left of the rotated image comes from the bottom-left of the original (blue).
        Assert.AreEqual(new Rgba32(0, 0, 255), decoded[0, 0]);
    }
}

[TestClass]
public sealed class BmpCodecTests
{
    [TestMethod]
    public void RoundTripsOpaqueImage()
    {
        using var source = TestImages.Gradient(33, 17);
        var encoded = BmpEncoder.Encode(source);
        using var decoded = BmpDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded));
    }

    [TestMethod]
    public void RoundTripsAlphaImage()
    {
        using var source = TestImages.Translucent(9, 7);
        var encoded = BmpEncoder.Encode(source);
        using var decoded = BmpDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded));
    }

    [TestMethod]
    public void IdentifiesDimensions()
    {
        using var source = TestImages.Gradient(11, 5);
        var encoded = BmpEncoder.Encode(source);
        var info = new BmpDecoder().Identify(encoded.Data)!;
        Assert.AreEqual(11, info.Width);
        Assert.AreEqual(5, info.Height);
    }
}

[TestClass]
public sealed class JpegCodecTests
{
    [TestMethod]
    public void RoundTripsGradientWithinToleranceAtHighQuality()
    {
        using var source = TestImages.Gradient(64, 48);
        var encoded = JpegEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.95 });
        Assert.AreEqual(ImageFormat.Jpeg, ImageFormats.Detect(encoded.Data));
        using var decoded = JpegDecoder.Decode(encoded.Data);
        Assert.AreEqual(source.Width, decoded.Width);
        Assert.AreEqual(source.Height, decoded.Height);
        Assert.IsTrue(TestImages.MeanChannelDifference(source, decoded) < 3.0, $"Mean difference {TestImages.MeanChannelDifference(source, decoded):0.##} is too high.");
    }

    [TestMethod]
    public void DecodesOwnSubsampledOutput()
    {
        using var source = TestImages.Gradient(70, 34);
        var encoded = JpegEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.8 });
        using var decoded = JpegDecoder.Decode(encoded.Data);
        Assert.AreEqual(70, decoded.Width);
        Assert.AreEqual(34, decoded.Height);
        Assert.IsTrue(TestImages.MeanChannelDifference(source, decoded) < 8.0);
    }

    [TestMethod]
    public void LowerQualityProducesSmallerFiles()
    {
        using var source = TestImages.Gradient(128, 128);
        var high = JpegEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.95 });
        var low = JpegEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.4 });
        Assert.IsTrue(low.Length < high.Length, $"low={low.Length} high={high.Length}");
    }

    [TestMethod]
    public void CompositesTransparencyOverBackground()
    {
        using var source = ImageBuffer.Create(16, 16, Rgba32.Transparent);
        var encoded = JpegEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.95, Background = new Rgba32(255, 0, 0) });
        using var decoded = JpegDecoder.Decode(encoded.Data);
        var p = decoded[8, 8];
        Assert.IsTrue(p.R > 200 && p.G < 60 && p.B < 60, $"Expected red background, got {p}");
        Assert.AreEqual(255, p.A);
    }

    [TestMethod]
    public void IdentifyReadsDimensions()
    {
        using var source = TestImages.Gradient(37, 21);
        var encoded = JpegEncoder.Encode(source);
        var info = new JpegDecoder().Identify(encoded.Data)!;
        Assert.AreEqual(37, info.Width);
        Assert.AreEqual(21, info.Height);
    }

    [TestMethod]
    public void WritesAndReadsExifWithPreservePolicy()
    {
        using var source = TestImages.Gradient(16, 16);
        source.Metadata = new ImageMetadata(new ExifData().WithPrimary(new ExifDirectory().With(ExifTag.Model, ExifValue.FromString("Test Model"))));
        var encoded = JpegEncoder.Encode(source, new ImageExportOptions { Format = ImageFormat.Jpeg, Metadata = MetadataPolicy.Preserve });
        using var decoded = JpegDecoder.Decode(encoded.Data);
        Assert.AreEqual("Test Model", decoded.Metadata?.Exif?.Model);
    }

    [TestMethod]
    public void RejectsGarbage()
    {
        var garbage = new byte[512];
        garbage[0] = 0xFF; garbage[1] = 0xD8; garbage[2] = 0xFF;
        Assert.ThrowsExactly<ImageDecodeException>(() => JpegDecoder.Decode(garbage));
    }
}

[TestClass]
public sealed class FormatDetectionTests
{
    [TestMethod]
    [DataRow("image/jpeg", ImageFormat.Jpeg)]
    [DataRow("image/png", ImageFormat.Png)]
    [DataRow("image/webp", ImageFormat.WebP)]
    [DataRow("image/avif", ImageFormat.Avif)]
    [DataRow("image/svg+xml", ImageFormat.Svg)]
    [DataRow("image/jpeg; charset=binary", ImageFormat.Jpeg)]
    [DataRow("nonsense", ImageFormat.Unknown)]
    public void MapsMimeTypes(string mime, ImageFormat expected) => Assert.AreEqual(expected, ImageFormats.FromMimeType(mime));

    [TestMethod]
    [DataRow("photo.JPG", ImageFormat.Jpeg)]
    [DataRow("a.b.webp", ImageFormat.WebP)]
    [DataRow(".png", ImageFormat.Png)]
    [DataRow("noext", ImageFormat.Unknown)]
    public void MapsFileNames(string name, ImageFormat expected) => Assert.AreEqual(expected, ImageFormats.FromFileName(name));

    [TestMethod]
    public void DetectsWebPMagic()
    {
        var data = new byte[16];
        "RIFF"u8.CopyTo(data);
        "WEBP"u8.CopyTo(data.AsSpan(8));
        Assert.AreEqual(ImageFormat.WebP, ImageFormats.Detect(data));
    }

    [TestMethod]
    public void DetectsAvifBrand()
    {
        var data = new byte[]
        {
            0, 0, 0, 0x1C, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'a', (byte)'v', (byte)'i', (byte)'f',
            0, 0, 0, 0, (byte)'a', (byte)'v', (byte)'i', (byte)'f',
        };
        Assert.AreEqual(ImageFormat.Avif, ImageFormats.Detect(data));
    }

    [TestMethod]
    public void DetectsSvg()
    {
        Assert.AreEqual(ImageFormat.Svg, ImageFormats.Detect("  <svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8));
        Assert.AreEqual(ImageFormat.Svg, ImageFormats.Detect("<?xml version=\"1.0\"?><svg/>"u8));
    }

    [TestMethod]
    public void ReturnsUnknownForEmptyOrShortData()
    {
        Assert.AreEqual(ImageFormat.Unknown, ImageFormats.Detect([]));
        Assert.AreEqual(ImageFormat.Unknown, ImageFormats.Detect([0x00, 0x01]));
    }
}

[TestClass]
public sealed class GifCodecTests
{
    // A 4x4 GIF89a: 2x2 blocks of red, green, blue, white, LZW encoded with a 4-entry global palette.
    private static byte[] SampleGif =>
    [
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x04, 0x00, 0x04, 0x00, 0x81, 0x00, 0x00,
        0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF,
        0x2C, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x04, 0x00, 0x00,
        0x02, 0x07, 0x04, 0x12, 0x86, 0x22, 0x33, 0xEC, 0x0A, 0x00, 0x3B,
    ];

    [TestMethod]
    public void DecodesFirstFrame()
    {
        using var img = GifDecoder.Decode(SampleGif);
        Assert.AreEqual(4, img.Width);
        Assert.AreEqual(4, img.Height);
        Assert.AreEqual(new Rgba32(255, 0, 0), img[0, 0]);
        Assert.AreEqual(new Rgba32(0, 255, 0), img[3, 0]);
        Assert.AreEqual(new Rgba32(0, 0, 255), img[0, 3]);
        Assert.AreEqual(new Rgba32(255, 255, 255), img[3, 3]);
    }

    [TestMethod]
    public void IdentifiesDimensions()
    {
        var info = new GifDecoder().Identify(SampleGif)!;
        Assert.AreEqual(4, info.Width);
        Assert.AreEqual(4, info.Height);
    }
}
