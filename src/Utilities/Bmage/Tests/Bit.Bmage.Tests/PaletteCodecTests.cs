using System.Buffers.Binary;
using System.Drawing;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Gif;
using Bit.Bmage.Codecs.Ico;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class IndexedPngTests
{
    /// <summary>A logo-like image: a few flat colours, some translucent, on transparency.</summary>
    internal static ImageBuffer Flat(int width = 90, int height = 60)
    {
        var image = ImageBuffer.Create(width, height, Rgba32.Transparent);
        image.Fill(new Rectangle(5, 5, 40, 30), new Rgba32(220, 30, 30));
        image.Fill(new Rectangle(30, 20, 40, 30), new Rgba32(20, 120, 220, 140));
        image.Fill(new Rectangle(60, 10, 25, 45), new Rgba32(250, 200, 0));
        return image;
    }

    private static int BitDepth(byte[] png) => png[24];

    private static int ColorType(byte[] png) => png[25];

    [TestMethod]
    public void AnImageWithFewColoursKeepsEveryPixelExactly()
    {
        using var flat = Flat();
        var encoded = PngEncoder.Encode(flat, new ImageExportOptions { MaxColors = 16 });
        Assert.AreEqual(3, ColorType(encoded.Data), "Indexed colour.");
        Assert.AreEqual(2, BitDepth(encoded.Data), "Three colours and transparency fit in two bits a pixel.");
        using var decoded = PngDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(flat, decoded));
        var full = PngEncoder.Encode(flat);
        Assert.IsLessThan(full.Length, encoded.Length, "The palette makes the file smaller.");
    }

    [TestMethod]
    [DataRow(2, 1)]
    [DataRow(4, 2)]
    [DataRow(16, 4)]
    [DataRow(17, 8)]
    [DataRow(256, 8)]
    public void TheFewestBitsThePaletteNeedsAreUsed(int colours, int bits)
    {
        using var image = ImageBuffer.Create(colours, 3);
        for (var x = 0; x < colours; x++)
            for (var y = 0; y < 3; y++) image[x, y] = new Rgba32((byte)x, (byte)(255 - x), (byte)(x * 7));
        var encoded = PngEncoder.Encode(image, new ImageExportOptions { MaxColors = 256 });
        Assert.AreEqual(bits, BitDepth(encoded.Data));
        using var decoded = PngDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(image, decoded), "Rows pack and unpack the indices exactly, whatever the width.");
    }

    [TestMethod]
    public void APhotoIsReducedToThePaletteCloseToItsColours()
    {
        using var photo = ImagePipeline.Create().Blur(2).Execute(ParallelProcessingTests.Noise(200, 150, seed: 2));
        foreach (var dither in new[] { false, true })
        {
            var encoded = PngEncoder.Encode(photo, new ImageExportOptions { MaxColors = 64, Dither = dither });
            using var decoded = PngDecoder.Decode(encoded.Data);
            var colours = new HashSet<Rgba32>();
            foreach (var p in decoded.Pixels) colours.Add(p);
            Assert.IsLessThanOrEqualTo(64, colours.Count, $"dither {dither}");
            Assert.IsLessThan(10, TestImages.MeanChannelDifference(photo, decoded, includeAlpha: true), $"dither {dither}");
        }
    }

    [TestMethod]
    public void DitheringBreaksUpBandsInAGradient()
    {
        // A slow grey ramp in eight levels bands into stripes; dithered, each stripe's average stays near the ramp.
        using var ramp = ImageBuffer.Create(256, 16);
        for (var x = 0; x < 256; x++)
            for (var y = 0; y < 16; y++) ramp[x, y] = new Rgba32((byte)x, (byte)x, (byte)x);
        double Error(bool dither)
        {
            using var decoded = PngDecoder.Decode(PngEncoder.Encode(ramp, new ImageExportOptions { MaxColors = 8, Dither = dither }).Data);
            // The column averages of the result against the ramp: what the eye sees from a distance.
            double total = 0;
            for (var x = 0; x < 256; x++)
            {
                double sum = 0;
                for (var y = 0; y < 16; y++) sum += decoded[x, y].R;
                total += Math.Abs(sum / 16 - x);
            }
            return total / 256;
        }
        var banded = Error(dither: false);
        var dithered = Error(dither: true);
        Assert.IsLessThan(banded / 2, dithered, $"banded {banded}, dithered {dithered}");
    }

    [TestMethod]
    public void MaxColorsIsChecked()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImageExportOptions { MaxColors = 1 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImageExportOptions { MaxColors = 257 });
        Assert.IsNull(new ImageExportOptions { MaxColors = null }.MaxColors);
    }

    [TestMethod]
    public void MetadataIsKeptAsThePolicySays()
    {
        using var flat = Flat();
        flat.Metadata = new Metadata.ImageMetadata(xmp: "<x:xmpmeta xmlns:x='adobe:ns:meta/'/>"u8.ToArray());
        var kept = PngEncoder.Encode(flat, new ImageExportOptions { MaxColors = 16, Metadata = Metadata.MetadataPolicy.Preserve });
        var removed = PngEncoder.Encode(flat, new ImageExportOptions { MaxColors = 16 });
        Assert.IsGreaterThan(removed.Length, kept.Length);
        using var decoded = PngDecoder.Decode(kept.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(flat, decoded));
    }
}

[TestClass]
public sealed class GifEncoderTests
{
    [TestMethod]
    public void AFlatImageRoundTripsWithItsTransparency()
    {
        using var flat = IndexedPngTests.Flat();
        var encoded = GifEncoder.Encode(flat);
        Assert.AreEqual(ImageFormat.Gif, ImageFormats.Detect(encoded.Data));
        using var decoded = GifDecoder.Decode(encoded.Data);
        // GIF has no partial transparency: the translucent blue (alpha 140) becomes opaque, everything else stays.
        for (var y = 0; y < flat.Height; y++)
            for (var x = 0; x < flat.Width; x++)
            {
                var p = flat[x, y];
                var expected = p.A == 0 ? Rgba32.Transparent : p.WithAlpha(255);
                Assert.AreEqual(expected, decoded[x, y].A == 0 ? Rgba32.Transparent : decoded[x, y], $"({x},{y})");
            }
    }

    [TestMethod]
    [DataRow(1, 1, 2)]
    [DataRow(7, 3, 2)]
    [DataRow(300, 200, 256)]
    [DataRow(640, 480, 256)]
    [DataRow(97, 61, 5)]
    public void EveryCodeWidthAndATableResetDecodeToTheQuantizedPixels(int width, int height, int colours)
    {
        // Noise defeats LZW's dictionary, so the codes widen to twelve bits and the table fills and clears many times.
        using var noise = ParallelProcessingTests.Noise(width, height, seed: width);
        var options = new ImageExportOptions { Format = ImageFormat.Gif, MaxColors = colours, Dither = false };
        var expected = ColorQuantizer.Quantize(noise, colours, dither: false, alphaLevels: 2);
        using var decoded = GifDecoder.Decode(GifEncoder.Encode(noise, options).Data);
        for (var i = 0; i < noise.PixelCount; i++)
        {
            var want = expected.Palette[expected.Indices[i]];
            var got = decoded.Pixels[i];
            if (want.A == 0) Assert.AreEqual(0, got.A, $"pixel {i}");
            else Assert.AreEqual(want, got, $"pixel {i}");
        }
    }

    [TestMethod]
    public void ARepetitiveImageCompressesWell()
    {
        using var stripes = ImageBuffer.Create(400, 300);
        for (var y = 0; y < 300; y++)
            for (var x = 0; x < 400; x++) stripes[x, y] = (x / 10 + y / 10) % 2 == 0 ? Rgba32.White : Rgba32.Black;
        var encoded = GifEncoder.Encode(stripes);
        Assert.IsLessThan(stripes.PixelCount / 20, encoded.Length);
        using var decoded = GifDecoder.Decode(encoded.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(stripes, decoded));
    }

    [TestMethod]
    public async Task TheProcessorEncodesGifAndIcoThroughItsRegistry()
    {
        var processor = new ImageProcessor();
        using var flat = IndexedPngTests.Flat(64, 64);
        var gif = await processor.EncodeAsync(flat, new ImageExportOptions { Format = ImageFormat.Gif });
        Assert.AreEqual(ImageFormat.Gif, gif.Format);
        Assert.AreEqual("image/gif", gif.MimeType);
        var ico = await processor.EncodeAsync(flat, new ImageExportOptions { Format = ImageFormat.Ico });
        Assert.AreEqual(ImageFormat.Ico, ImageFormats.Detect(ico.Data));
        Assert.AreEqual(".ico", ico.Extension);
        using var back = await processor.DecodeAsync(ico.Data);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(flat, back));
        Assert.IsTrue(processor.Codecs.CanEncode(ImageFormat.Gif));
    }

    [TestMethod]
    public void AnImageTooLargeForGifIsRefused()
    {
        using var wide = ImageBuffer.Create(70000, 1, limits: ImageLimits.Unrestricted);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => GifEncoder.Encode(wide));
    }
}

[TestClass]
public sealed class IcoCodecTests
{
    [TestMethod]
    public void AFaviconHoldsEachSizeAndDecodesToTheLargest()
    {
        using var logo = IndexedPngTests.Flat(120, 80);
        var favicon = IcoEncoder.CreateFavicon(logo);
        Assert.AreEqual(ImageFormat.Ico, ImageFormats.Detect(favicon.Data));
        var entries = IcoDecoder.ReadDirectory(favicon.Data);
        CollectionAssert.AreEqual(new[] { 16, 32, 48 }, entries.Select(e => e.Width).ToArray());
        CollectionAssert.AreEqual(new[] { 16, 32, 48 }, entries.Select(e => e.Height).ToArray());

        var info = new IcoDecoder().Identify(favicon.Data);
        Assert.AreEqual(48, info!.Width);
        using var decoded = IcoDecoder.Decode(favicon.Data);
        Assert.AreEqual(new Size(48, 48), decoded.Size);
        // Fitted, not stretched: the 3:2 logo leaves transparent bands above and below.
        Assert.AreEqual(0, decoded[24, 2].A);
        Assert.AreEqual(ImageFormat.Ico, decoded.Metadata!.SourceFormat);
    }

    [TestMethod]
    public void A256PixelImageIsWrittenWithTheDirectorysZero()
    {
        using var big = ImageBuffer.Create(256, 256, new Rgba32(10, 20, 30));
        var encoded = IcoEncoder.Encode([big]);
        Assert.AreEqual(0, encoded.Data[6], "256 is stored as 0.");
        Assert.AreEqual(256, IcoDecoder.ReadDirectory(encoded.Data)[0].Width);
        using var tooBig = ImageBuffer.Create(257, 16);
        Assert.ThrowsExactly<ArgumentException>(() => IcoEncoder.Encode([tooBig]));
        Assert.ThrowsExactly<ArgumentException>(() => IcoEncoder.Encode([]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => IcoEncoder.CreateFavicon(big, [0]));
    }

    /// <summary>An icon whose one image is a bitmap: the header, an optional palette, bottom-up rows, and the mask.</summary>
    private static byte[] BitmapIcon(int width, int height, int bpp, Func<int, int, byte[]> pixel, Func<int, int, bool> transparent, Rgba32[]? palette = null, bool withMask = true)
    {
        var stride = (width * bpp + 31) / 32 * 4;
        var maskStride = (width + 31) / 32 * 4;
        var paletteBytes = (palette?.Length ?? 0) * 4;
        var dib = new byte[40 + paletteBytes + stride * height + (withMask ? maskStride * height : 0)];
        BinaryPrimitives.WriteInt32LittleEndian(dib, 40);
        BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(4), width);
        BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(8), height * 2);
        BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(14), (ushort)bpp);
        if (palette is not null)
        {
            BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(32), palette.Length);
            for (var i = 0; i < palette.Length; i++)
            {
                dib[40 + i * 4] = palette[i].B;
                dib[40 + i * 4 + 1] = palette[i].G;
                dib[40 + i * 4 + 2] = palette[i].R;
            }
        }
        var pixels = 40 + paletteBytes;
        for (var y = 0; y < height; y++)
        {
            var row = pixels + (height - 1 - y) * stride;
            for (var x = 0; x < width; x++)
            {
                var value = pixel(x, y);
                if (bpp >= 8) value.CopyTo(dib, row + x * (bpp / 8));
                else dib[row + x * bpp / 8] |= (byte)(value[0] << (8 - bpp - x * bpp % 8));
            }
            if (!withMask) continue;
            var mask = pixels + stride * height + (height - 1 - y) * maskStride;
            for (var x = 0; x < width; x++)
                if (transparent(x, y)) dib[mask + x / 8] |= (byte)(0x80 >> (x % 8));
        }
        var file = new byte[6 + 16 + dib.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(4), 1);
        file[6] = (byte)width;
        file[7] = (byte)height;
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(10), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(12), (ushort)bpp);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(14), dib.Length);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(18), 22);
        dib.CopyTo(file, 22);
        return file;
    }

    [TestMethod]
    public void ABitmapIconWithAPaletteUsesItsMaskForTransparency()
    {
        Rgba32[] palette = [new(255, 0, 0), new(0, 0, 255)];
        var data = BitmapIcon(10, 6, 1, (x, _) => [(byte)(x % 2)], (x, y) => x == 0 || y == 0, palette);
        Assert.AreEqual(ImageFormat.Ico, ImageFormats.Detect(data));
        using var image = IcoDecoder.Decode(data);
        Assert.AreEqual(new Size(10, 6), image.Size);
        Assert.AreEqual(0, image[0, 3].A);
        Assert.AreEqual(0, image[5, 0].A);
        Assert.AreEqual(new Rgba32(0, 0, 255), image[1, 3]);
        Assert.AreEqual(new Rgba32(255, 0, 0), image[2, 3]);
    }

    [TestMethod]
    public void A32BitIconUsesItsAlphaAndAMasklessOneIsAccepted()
    {
        var data = BitmapIcon(4, 4, 32, (x, y) => [10, 20, 30, (byte)(x * 60)], (_, _) => true, withMask: false);
        using var image = IcoDecoder.Decode(data);
        Assert.AreEqual(new Rgba32(30, 20, 10, 120), image[2, 1]);
        Assert.AreEqual(0, image[0, 0].A, "Alpha decides, not the absent mask.");
        // No alpha at all, and a mask: the mask decides.
        var masked = BitmapIcon(4, 4, 32, (_, _) => [10, 20, 30, 0], (x, _) => x < 2);
        using var image2 = IcoDecoder.Decode(masked);
        Assert.AreEqual(0, image2[1, 1].A);
        Assert.AreEqual(new Rgba32(30, 20, 10), image2[3, 1]);
        var opaque = BitmapIcon(3, 3, 24, (_, _) => [1, 2, 3], (_, _) => false);
        using var image3 = IcoDecoder.Decode(opaque);
        Assert.AreEqual(new Rgba32(3, 2, 1), image3[1, 1]);
    }

    [TestMethod]
    public void AMalformedIconIsRefusedAsADecodeError()
    {
        var good = IcoEncoder.CreateFavicon(IndexedPngTests.Flat(32, 32), [16]).Data;
        var noImages = (byte[])good.Clone();
        noImages[4] = 0;
        Assert.AreNotEqual(ImageFormat.Ico, ImageFormats.Detect(noImages), "A directory of no images is not an icon.");

        var outside = (byte[])good.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(outside.AsSpan(18), good.Length);
        Assert.ThrowsExactly<ImageDecodeException>(() => IcoDecoder.Decode(outside));
        Assert.IsNull(new IcoDecoder().Identify(outside));

        var tooLong = (byte[])good.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(tooLong.AsSpan(14), int.MaxValue);
        Assert.ThrowsExactly<ImageDecodeException>(() => IcoDecoder.Decode(tooLong));

        var truncated = good[..30];
        Assert.ThrowsExactly<ImageDecodeException>(() => IcoDecoder.Decode(truncated));

        var hugeBitmap = BitmapIcon(4, 4, 32, (_, _) => [0, 0, 0, 255], (_, _) => false);
        BinaryPrimitives.WriteInt32LittleEndian(hugeBitmap.AsSpan(22 + 4), 100_000);
        Assert.ThrowsExactly<ImageDecodeException>(() => IcoDecoder.Decode(hugeBitmap));
    }

    [TestMethod]
    public void TheFormatIsNamedAndDetected()
    {
        Assert.AreEqual("image/x-icon", ImageFormat.Ico.GetMimeType());
        Assert.AreEqual(ImageFormat.Ico, ImageFormats.FromMimeType("image/vnd.microsoft.icon"));
        Assert.AreEqual(ImageFormat.Ico, ImageFormats.FromFileName("favicon.ico"));
        Assert.IsTrue(ImageFormat.Ico.SupportsAlpha());
        // An AVIF whose first box is 256 bytes long starts with the same four bytes as an icon.
        var avif = new byte[300];
        avif[2] = 1;
        "ftypavif"u8.CopyTo(avif.AsSpan(4));
        Assert.AreEqual(ImageFormat.Avif, ImageFormats.Detect(avif));
    }
}
