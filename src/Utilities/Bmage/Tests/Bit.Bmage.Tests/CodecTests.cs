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

    [TestMethod]
    public void ABaselineJpegRepeatingItsScanStopsAtTheScansItsFrameAllows()
    {
        using var source = TestImages.Gradient(32, 24);
        var encoded = JpegEncoder.Encode(source).Data;
        var sos = encoded.AsSpan().IndexOf(new byte[] { 0xFF, 0xDA });
        var header = encoded.AsSpan(sos, 2 + (encoded[sos + 2] << 8 | encoded[sos + 3])).ToArray();
        var withScans = (int extra) => encoded[..^2].Concat(Enumerable.Repeat(header, extra).SelectMany(h => h)).Concat(new byte[] { 0xFF, 0xD9 }).ToArray();

        // Scans past one per component are ignored, however many there are, and the image decoded by then is kept.
        using var allowed = JpegDecoder.Decode(withScans(2));
        foreach (var extra in new[] { 3, 10_000 })
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            using var decoded = JpegDecoder.Decode(withScans(extra));
            Assert.IsLessThan(2000, watch.ElapsedMilliseconds, $"{extra} extra scans took {watch.ElapsedMilliseconds} ms.");
            Assert.AreEqual(0, TestImages.MaxChannelDifference(allowed, decoded), $"{extra} extra scans");
        }
    }

    [TestMethod]
    public void AProgressiveJpegWithAbsurdlyManyScansStopsAtTheCap()
    {
        static byte[] Progressive(int scans)
        {
            var jpeg = new List<byte> { 0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x43, 0x00 };
            jpeg.AddRange(Enumerable.Repeat((byte)1, 64));
            jpeg.AddRange([0xFF, 0xC2, 0x00, 0x0B, 0x08, 0x00, 0x08, 0x00, 0x08, 0x01, 0x01, 0x11, 0x00]);
            jpeg.AddRange([0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x00]);
            for (var i = 0; i < scans; i++) jpeg.AddRange([0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00]);
            jpeg.AddRange([0xFF, 0xD9]);
            return [.. jpeg];
        }

        using var capped = JpegDecoder.Decode(Progressive(500));
        Assert.AreEqual(8, capped.Width);
        using var past = JpegDecoder.Decode(Progressive(501));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(capped, past));
    }

    /// <summary>FF00 is a stuffed byte, which decoders skip; read as a marker, its "length" jumped over the frame header.</summary>
    [TestMethod]
    public void IdentifySkipsAStuffedByteBeforeTheFrameHeader()
    {
        using var source = TestImages.Gradient(37, 21);
        var encoded = JpegEncoder.Encode(source).Data;
        var sof = encoded.AsSpan().IndexOf(new byte[] { 0xFF, 0xC0 });
        var stuffed = encoded[..sof].Concat(new byte[] { 0xFF, 0x00, 0x12, 0x34 }).Concat(encoded[sof..]).ToArray();

        var info = new JpegDecoder().Identify(stuffed)!;
        Assert.AreEqual(37, info.Width);
        Assert.AreEqual(21, info.Height);
        using var decoded = JpegDecoder.Decode(stuffed);
        Assert.AreEqual(37, decoded.Width);
    }

    [TestMethod]
    public void ReadOrientationAgreesWithReadMetadata()
    {
        using var source = TestImages.Gradient(16, 8);
        var plain = JpegEncoder.Encode(source).Data;
        foreach (var orientation in new[] { Orientation.Normal, Orientation.Rotate90, Orientation.FlipHorizontal, Orientation.Rotate270 })
        {
            var exif = ExifData.FromOrientation(orientation);
            var encoded = JpegContainer.WriteMetadata(plain, new ImageMetadata(exif.WithPrimary(exif.Primary.With(ExifTag.Model, ExifValue.FromString("Cam")))));
            Assert.AreEqual(orientation, JpegContainer.ReadOrientation(encoded), orientation.ToString());
            Assert.AreEqual(JpegContainer.ReadMetadata(encoded).Orientation, JpegContainer.ReadOrientation(encoded), orientation.ToString());
        }
        Assert.AreEqual(Orientation.Normal, JpegContainer.ReadOrientation(plain));
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
    public void AFrameOverhangingTheLogicalScreenIsClippedToIt()
    {
        var shifted = SampleGif;
        shifted[26] = 1;
        using (var img = GifDecoder.Decode(shifted))
        {
            Assert.AreEqual(4, img.Width);
            Assert.AreEqual(4, img.Height);
            Assert.AreEqual(new Rgba32(0, 0, 0, 0), img[0, 0]);
            Assert.AreEqual(new Rgba32(255, 0, 0), img[1, 0]);
            Assert.AreEqual(new Rgba32(0, 255, 0), img[3, 0]);
        }

        // However small the logical screen, the frame itself is still held to the pixel limit.
        var huge = SampleGif;
        huge[30] = 0xD0; huge[31] = 0x07; huge[32] = 0xD0; huge[33] = 0x07;
        using (var img = GifDecoder.Decode(huge)) Assert.AreEqual(4, img.Width);
        Assert.ThrowsExactly<ImageLimitExceededException>(() => GifDecoder.Decode(huge, DecodeOptions.Default with { Limits = ImageLimits.Default with { MaxPixels = 1_000_000 } }));

        // And to the width and height limits, which a long thin frame stays under the pixel limit by.
        var wide = SampleGif;
        wide[30] = 0xFF; wide[31] = 0xFF; wide[32] = 0x10; wide[33] = 0x00;
        using (var img = GifDecoder.Decode(wide, DecodeOptions.Default with { Limits = ImageLimits.Unrestricted })) Assert.AreEqual(4, img.Width);
        Assert.ThrowsExactly<ImageLimitExceededException>(() => GifDecoder.Decode(wide));
    }

    /// <summary>
    /// The frame is decoded a row at a time, with only the screen's part of each row kept, so codes that run across
    /// rows and a frame overhanging the screen both come out as they were encoded.
    /// </summary>
    [TestMethod]
    public void AFrameDecodedARowAtATimeKeepsEveryIndex()
    {
        static Rgba32 Colour(int i) => new((byte)i, (byte)(255 - i), (byte)(i * 3));
        var patterns = new Func<int, int, byte>[]
        {
            (x, y) => (byte)((x * 7 + y * 13) % 256),
            // Long runs, so single codes stand for many indices and run on from one row into the next.
            (x, y) => (byte)((x / 9 + y / 4) % 3),
        };

        foreach (var index in patterns)
            foreach (var (screenWidth, screenHeight) in new[] { (40, 30), (10, 10), (1, 1) })
            {
                using var img = GifDecoder.Decode(Gif(screenWidth, screenHeight, 40, 30, index));
                Assert.AreEqual(screenWidth, img.Width);
                for (var y = 0; y < screenHeight; y++)
                    for (var x = 0; x < screenWidth; x++)
                        Assert.AreEqual(Colour(index(x, y)), img[x, y], $"({x}, {y}) on a {screenWidth}x{screenHeight} screen");
            }
    }

    /// <summary>A GIF of one LZW coded frame at the screen's origin, with a 256 colour palette.</summary>
    private static byte[] Gif(int screenWidth, int screenHeight, int frameWidth, int frameHeight, Func<int, int, byte> index)
    {
        var gif = new List<byte>("GIF89a"u8.ToArray());
        gif.AddRange([(byte)screenWidth, (byte)(screenWidth >> 8), (byte)screenHeight, (byte)(screenHeight >> 8), 0xF7, 0, 0]);
        for (var i = 0; i < 256; i++) gif.AddRange([(byte)i, (byte)(255 - i), (byte)(i * 3)]);
        gif.AddRange([0x2C, 0, 0, 0, 0, (byte)frameWidth, (byte)(frameWidth >> 8), (byte)frameHeight, (byte)(frameHeight >> 8), 0, 8]);

        const int clear = 256, end = 257;
        var bits = new List<byte>();
        ulong buffer = 0;
        int count = 0, codeSize = 9, next = end + 1;
        void Emit(int code)
        {
            buffer |= (ulong)code << count;
            for (count += codeSize; count >= 8; count -= 8, buffer >>= 8) bits.Add((byte)buffer);
        }
        var table = new Dictionary<(int Prefix, byte Index), int>();
        Emit(clear);
        var prefix = -1;
        for (var y = 0; y < frameHeight; y++)
            for (var x = 0; x < frameWidth; x++)
            {
                var k = index(x, y);
                if (prefix < 0) { prefix = k; continue; }
                if (table.TryGetValue((prefix, k), out var code)) { prefix = code; continue; }
                Emit(prefix);
                if (next < 4096)
                {
                    table[(prefix, k)] = next++;
                    if (next > 1 << codeSize && codeSize < 12) codeSize++;
                }
                else
                {
                    Emit(clear);
                    table.Clear();
                    (codeSize, next) = (9, end + 1);
                }
                prefix = k;
            }
        Emit(prefix);
        Emit(end);
        if (count > 0) bits.Add((byte)buffer);
        for (var i = 0; i < bits.Count; i += 255)
        {
            var length = Math.Min(255, bits.Count - i);
            gif.Add((byte)length);
            gif.AddRange(bits.GetRange(i, length));
        }
        gif.AddRange([0, 0x3B]);
        return [.. gif];
    }

    [TestMethod]
    public void IdentifiesDimensions()
    {
        var info = new GifDecoder().Identify(SampleGif)!;
        Assert.AreEqual(4, info.Width);
        Assert.AreEqual(4, info.Height);
    }
}
