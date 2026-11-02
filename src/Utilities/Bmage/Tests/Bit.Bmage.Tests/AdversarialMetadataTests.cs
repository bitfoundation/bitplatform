using System.Buffers.Binary;
using System.Drawing;
using Bit.Bmage.Codecs;
using Bit.Bmage.Geometry;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Tests;

/// <summary>
/// EXIF parsing, orientation and privacy tests. Metadata comes from files the application did not write, so both the
/// correctness of the parse and its behaviour on hostile input matter.
/// </summary>
[TestClass]
public sealed class AdversarialMetadataTests
{
    /// <summary>
    /// Builds a minimal TIFF/EXIF block containing one IFD0 with the given tags, in either byte order. Rationals are
    /// written as (numerator, denominator) pairs.
    /// </summary>
    private static byte[] BuildTiff(bool bigEndian, params (ushort Tag, ExifType Type, uint[] Values)[] entries)
    {
        var header = new byte[8];
        if (bigEndian)
        {
            header[0] = (byte)'M'; header[1] = (byte)'M';
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(2), 0x2A);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 8);
        }
        else
        {
            header[0] = (byte)'I'; header[1] = (byte)'I';
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), 0x2A);
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), 8);
        }

        var ifdSize = 2 + entries.Length * 12 + 4;
        var body = new MemoryStream();
        var overflow = new MemoryStream();
        var overflowBase = 8 + ifdSize;

        void W16(Span<byte> d, ushort v) { if (bigEndian) BinaryPrimitives.WriteUInt16BigEndian(d, v); else BinaryPrimitives.WriteUInt16LittleEndian(d, v); }
        void W32(Span<byte> d, uint v) { if (bigEndian) BinaryPrimitives.WriteUInt32BigEndian(d, v); else BinaryPrimitives.WriteUInt32LittleEndian(d, v); }

        Span<byte> count = stackalloc byte[2];
        W16(count, (ushort)entries.Length);
        body.Write(count);

        Span<byte> entry = stackalloc byte[12];
        foreach (var (tag, type, values) in entries)
        {
            var size = ExifValue.SizeOf(type);
            var componentCount = type is ExifType.Rational or ExifType.SRational ? values.Length / 2 : values.Length;
            var payload = new byte[componentCount * size];
            for (var i = 0; i < values.Length; i++)
            {
                var span = payload.AsSpan(i * (size == 8 ? 4 : size));
                if (size == 2) W16(span, (ushort)values[i]);
                else if (size == 1) span[0] = (byte)values[i];
                else W32(span, values[i]);
            }

            W16(entry, tag);
            W16(entry[2..], (ushort)type);
            W32(entry[4..], (uint)componentCount);
            entry[8..].Clear();
            if (payload.Length <= 4) payload.CopyTo(entry[8..]);
            else
            {
                W32(entry[8..], (uint)(overflowBase + overflow.Length));
                overflow.Write(payload);
            }
            body.Write(entry);
        }

        Span<byte> next = stackalloc byte[4];
        W32(next, 0);
        body.Write(next);

        var result = new MemoryStream();
        result.Write(header);
        body.Position = 0; body.CopyTo(result);
        overflow.Position = 0; overflow.CopyTo(result);
        return result.ToArray();
    }

    /// <summary>
    /// Rationals must decode identically whichever byte order the file uses. Cameras that write big-endian TIFF are
    /// common, and a rational holds exposure, aperture, focal length and every GPS coordinate.
    /// </summary>
    [TestMethod]
    [DataRow(1u, 200u)]
    [DataRow(7u, 5u)]
    [DataRow(1000u, 3u)]
    [DataRow(0u, 1u)]
    public void RationalsDecodeIdenticallyInBothByteOrders(uint numerator, uint denominator)
    {
        var little = ExifData.Parse(BuildTiff(false, (ExifTag.ExposureTime, ExifType.Rational, [numerator, denominator])));
        var big = ExifData.Parse(BuildTiff(true, (ExifTag.ExposureTime, ExifType.Rational, [numerator, denominator])));

        Assert.IsNotNull(little);
        Assert.IsNotNull(big);
        var expected = numerator / (double)denominator;
        Assert.AreEqual(expected, little!.Primary.Get(ExifTag.ExposureTime)!.GetDouble()!.Value, 0.5e-9);
        Assert.AreEqual(expected, big!.Primary.Get(ExifTag.ExposureTime)!.GetDouble()!.Value, 0.5e-9);
    }

    /// <summary>A multi-component rational array (GPS coordinates are three) must decode fully in both byte orders.</summary>
    [TestMethod]
    public void GpsStyleRationalArraysDecodeIdenticallyInBothByteOrders()
    {
        // 51 degrees, 30 minutes, 27.5 seconds.
        uint[] values = [51, 1, 30, 1, 275, 10];
        var little = ExifData.Parse(BuildTiff(false, (ExifTag.GpsLatitude, ExifType.Rational, values)))!;
        var big = ExifData.Parse(BuildTiff(true, (ExifTag.GpsLatitude, ExifType.Rational, values)))!;

        for (var i = 0; i < 3; i++)
        {
            var l = little.Primary.Get(ExifTag.GpsLatitude)!.GetDouble(i);
            var b = big.Primary.Get(ExifTag.GpsLatitude)!.GetDouble(i);
            Assert.AreEqual(l, b);
        }
        Assert.AreEqual(51d, big.Primary.Get(ExifTag.GpsLatitude)!.GetDouble(0));
        Assert.AreEqual(30d, big.Primary.Get(ExifTag.GpsLatitude)!.GetDouble(1));
        Assert.AreEqual(27.5d, big.Primary.Get(ExifTag.GpsLatitude)!.GetDouble(2));
    }

    [TestMethod]
    [DataRow(ExifType.Short)]
    [DataRow(ExifType.Long)]
    [DataRow(ExifType.Rational)]
    public void ScalarTypesDecodeIdenticallyInBothByteOrders(ExifType type)
    {
        uint[] values = type is ExifType.Rational ? [355u, 113u] : [1234u];
        var little = ExifData.Parse(BuildTiff(false, (ExifTag.Iso, type, values)))!;
        var big = ExifData.Parse(BuildTiff(true, (ExifTag.Iso, type, values)))!;
        Assert.AreEqual(little.Primary.Get(ExifTag.Iso)!.GetDouble(), big.Primary.Get(ExifTag.Iso)!.GetDouble());
    }

    /// <summary>Hostile EXIF must be ignored, not throw: it arrives inside files the application did not create.</summary>
    [TestMethod]
    public void HostileExifOffsetsAreIgnoredNotThrown()
    {
        var rnd = new Random(9182);
        var baseTiff = BuildTiff(false,
            (ExifTag.Orientation, ExifType.Short, [6u]),
            (ExifTag.ExposureTime, ExifType.Rational, [1u, 60u]),
            (ExifTag.Make, ExifType.Ascii, [65u, 66u, 67u, 0u]));

        for (var trial = 0; trial < 500; trial++)
        {
            var copy = baseTiff.ToArray();
            for (var flips = 0; flips < 5; flips++) copy[rnd.Next(copy.Length)] = (byte)rnd.Next(256);
            var ex = Capture.Exception(() => ExifData.Parse(copy));
            Assert.IsTrue(ex is null, $"corrupted EXIF threw {ex?.GetType().Name}: {ex?.Message}");
        }

        // Explicitly hostile offsets: values that would overflow a naive bounds check.
        foreach (var offset in new uint[] { uint.MaxValue, uint.MaxValue - 3, 0x7FFFFFFF, 0x80000000 })
        {
            var tiff = BuildTiff(false, (ExifTag.Make, ExifType.Ascii, [65u, 66u, 67u, 68u, 69u, 70u, 71u, 72u]));
            // Point the (out of line) value at a hostile offset.
            BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(8 + 2 + 8), offset);
            var ex = Capture.Exception(() => ExifData.Parse(tiff));
            Assert.IsTrue(ex is null, $"EXIF value offset {offset} threw {ex?.GetType().Name}");
        }
    }

    /// <summary>A PNG carrying hostile EXIF must still identify, because Identify is documented never to throw.</summary>
    [TestMethod]
    public async Task IdentifyToleratesHostileExif()
    {
        var processor = new ImageProcessor();
        using var img = AdversarialGeometryTests.Noise(16, 16, 5);
        var png = (await processor.EncodeAsync(img, ImageExportOptions.Png with { Metadata = MetadataPolicy.Preserve })).Data;

        var rnd = new Random(5150);
        for (var trial = 0; trial < 200; trial++)
        {
            var copy = png.ToArray();
            for (var flips = 0; flips < 4; flips++) copy[rnd.Next(copy.Length)] = (byte)rnd.Next(256);
            var ex = Capture.Exception(() => { processor.Identify(copy); });
            Assert.IsTrue(ex is null, $"Identify threw {ex?.GetType().Name}: {ex?.Message}");
        }
    }

    // ---------------------------------------------------------------- orientation matrix

    /// <summary>
    /// The full EXIF orientation matrix, end to end through a real JPEG: a decoder that auto-orients must produce the
    /// same pixels as applying the orientation by hand, and must reset the stored orientation so it is not applied twice.
    /// </summary>
    [TestMethod]
    [DataRow(Orientation.Normal)]
    [DataRow(Orientation.FlipHorizontal)]
    [DataRow(Orientation.Rotate180)]
    [DataRow(Orientation.FlipVertical)]
    [DataRow(Orientation.Transpose)]
    [DataRow(Orientation.Rotate90)]
    [DataRow(Orientation.Transverse)]
    [DataRow(Orientation.Rotate270)]
    public async Task AutoOrientProducesUprightPixelsForEveryExifOrientation(Orientation orientation)
    {
        var processor = new ImageProcessor();
        using var upright = TestImages.Quadrants(32, 24);

        // Store the image the way a camera would: pixels in sensor order, with the orientation tag saying how to
        // display them. Sensor order is the inverse of the display transform.
        using var stored = new OrientationOperation(orientation.Inverse()).Apply(upright, OperationContext.Default);
        stored.Metadata = new ImageMetadata(ExifData.FromOrientation(orientation), sourceFormat: ImageFormat.Jpeg);

        var encoded = await processor.EncodeAsync(stored, ImageExportOptions.Jpeg with
        {
            Quality = 1.0,
            Metadata = MetadataPolicy.Preserve,
        });

        using var decoded = await processor.DecodeAsync(encoded.Data, new DecodeOptions { AutoOrient = true });

        Assert.AreEqual(upright.Size, decoded.Size);
        Assert.IsTrue(TestImages.MaxChannelDifference(upright, decoded) < 40,
            $"{orientation}: auto-oriented pixels differ from the upright reference");
        // Applying it again must be a no-op, so the stored orientation has to be reset.
        Assert.IsTrue(decoded.Metadata?.Orientation is Orientation.Normal or Orientation.Unspecified,
            $"{orientation}: decoded image still reports orientation {decoded.Metadata?.Orientation}, so it would be applied twice");
    }

    /// <summary>
    /// Decoding without auto-orient leaves the pixels in sensor order and the tag describing them. Re-encoding must
    /// carry that tag through, or the image is silently saved sideways with nothing left to correct it.
    /// </summary>
    [TestMethod]
    [DataRow(Orientation.Rotate90)]
    [DataRow(Orientation.Rotate180)]
    [DataRow(Orientation.Rotate270)]
    [DataRow(Orientation.FlipHorizontal)]
    [DataRow(Orientation.Transpose)]
    public async Task ReEncodingWithoutAutoOrientKeepsTheOrientationTag(Orientation orientation)
    {
        var processor = new ImageProcessor();
        using var upright = TestImages.Quadrants(32, 24);
        using var stored = new OrientationOperation(orientation.Inverse()).Apply(upright, OperationContext.Default);
        stored.Metadata = new ImageMetadata(ExifData.FromOrientation(orientation), sourceFormat: ImageFormat.Jpeg);

        var camera = await processor.EncodeAsync(stored, ImageExportOptions.Jpeg with { Quality = 1.0, Metadata = MetadataPolicy.Preserve });

        // Round trip in sensor order, exactly what a "strip metadata but keep the picture" workflow does badly.
        using var raw = await processor.DecodeAsync(camera.Data, new DecodeOptions { AutoOrient = false });
        Assert.AreEqual(orientation, raw.Metadata!.Orientation);

        var reencoded = await processor.EncodeAsync(raw, ImageExportOptions.Jpeg with { Quality = 1.0, Metadata = MetadataPolicy.Preserve });
        using var final = await processor.DecodeAsync(reencoded.Data, new DecodeOptions { AutoOrient = true });

        Assert.AreEqual(upright.Size, final.Size);
        Assert.IsTrue(TestImages.MaxChannelDifference(upright, final) < 40,
            $"{orientation}: the orientation tag was lost across a re-encode, so the image is now sideways");
    }

    [TestMethod]
    public void AutoOrientIsIdempotent()
    {
        using var src = AdversarialGeometryTests.Noise(20, 14, 3);
        src.Metadata = new ImageMetadata(ExifData.FromOrientation(Orientation.Rotate90));
        using var once = AutoOrientOperation.Instance.Apply(src, OperationContext.Default);
        using var twice = AutoOrientOperation.Instance.Apply(once, OperationContext.Default);
        Assert.AreEqual(once.Size, twice.Size);
        Assert.IsTrue(once.Bytes.SequenceEqual(twice.Bytes), "auto-orient applied twice changed the image");
    }

    // ---------------------------------------------------------------- privacy

    private static ExifData BuildSensitiveExif()
    {
        var gps = new ExifDirectory()
            .With(ExifTag.GpsLatitude, ExifValue.FromRational((51, 1), (30, 1), (275, 10)))
            .With(ExifTag.GpsLatitudeRef, ExifValue.FromString("N"))
            .With(ExifTag.GpsLongitude, ExifValue.FromRational((0, 1), (7, 1), (390, 10)))
            .With(ExifTag.GpsLongitudeRef, ExifValue.FromString("W"));
        var exif = new ExifDirectory()
            .With(ExifTag.BodySerialNumber, ExifValue.FromString("SN-12345"))
            .With(ExifTag.LensSerialNumber, ExifValue.FromString("LENS-999"))
            .With(ExifTag.OwnerName, ExifValue.FromString("Jane Doe"))
            .With(ExifTag.UserComment, ExifValue.FromString("private note"))
            .With(ExifTag.ImageUniqueId, ExifValue.FromString("UID-abc"))
            .With(ExifTag.MakerNote, ExifValue.FromUndefined(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }))
            .With(ExifTag.DateTimeOriginal, ExifValue.FromString("2024:01:02 03:04:05"));
        var primary = new ExifDirectory()
            .With(ExifTag.Make, ExifValue.FromString("TestCam"))
            .With(ExifTag.Model, ExifValue.FromString("T1000"))
            .With(ExifTag.Copyright, ExifValue.FromString("(c) Jane"));
        return new ExifData(primary, exif, gps, null, new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
    }

    [TestMethod]
    public void SensitiveFieldsAreReadableBeforeStripping()
    {
        var exif = BuildSensitiveExif();
        Assert.IsTrue(exif.HasLocation);
        Assert.IsNotNull(exif.Location);
        Assert.IsFalse(exif.Thumbnail.IsEmpty);
    }

    [TestMethod]
    public void StripSensitiveRemovesLocationAndIdentifiersButKeepsDescriptiveFields()
    {
        var stripped = BuildSensitiveExif().StripSensitive();

        Assert.IsFalse(stripped.HasLocation);
        Assert.IsNull(stripped.Location);
        Assert.AreEqual(0, stripped.Gps.Count);
        Assert.IsTrue(stripped.Thumbnail.IsEmpty);
        foreach (var tag in new[] { ExifTag.BodySerialNumber, ExifTag.LensSerialNumber, ExifTag.OwnerName, ExifTag.UserComment, ExifTag.ImageUniqueId, ExifTag.MakerNote })
            Assert.IsFalse(stripped.Exif.Contains(tag), $"tag 0x{tag:X4} survived StripSensitive");

        Assert.AreEqual("TestCam", stripped.Primary.Get(ExifTag.Make)?.GetString());
        Assert.AreEqual("(c) Jane", stripped.Primary.Get(ExifTag.Copyright)?.GetString());
        Assert.AreEqual("2024:01:02 03:04:05", stripped.Exif.Get(ExifTag.DateTimeOriginal)?.GetString());
    }

    /// <summary>
    /// The end-to-end privacy guarantee: no byte of a stripped or removed export may contain GPS or identifier data.
    /// Checking the serialised file, not just the object model, is what makes this a real guarantee.
    /// </summary>
    [TestMethod]
    [DataRow(MetadataPolicy.Remove)]
    [DataRow(MetadataPolicy.StripSensitive)]
    public async Task ExportedBytesContainNoSensitiveMetadata(MetadataPolicy policy)
    {
        var processor = new ImageProcessor();
        using var img = AdversarialGeometryTests.Noise(32, 32, 11);
        img.Metadata = new ImageMetadata(BuildSensitiveExif(), sourceFormat: ImageFormat.Jpeg);

        foreach (var format in new[] { ImageFormat.Jpeg, ImageFormat.Png })
        {
            var encoded = await processor.EncodeAsync(img, new ImageExportOptions { Format = format, Metadata = policy });
            var text = System.Text.Encoding.Latin1.GetString(encoded.Data);
            foreach (var secret in new[] { "SN-12345", "LENS-999", "Jane Doe", "private note", "UID-abc" })
                Assert.DoesNotContain(secret, text, StringComparison.Ordinal);

            var round = ExifData.Parse(text.Length == 0 ? [] : encoded.Data);
            var reread = await processor.DecodeAsync(encoded.Data);
            using (reread)
            {
                var gps = reread.Metadata?.Exif?.Gps;
                Assert.IsTrue(gps is null || gps.Count == 0, $"{format}/{policy} kept {gps?.Count} GPS tags");
                Assert.IsNull(reread.Metadata?.Exif?.Location);
            }
            _ = round;
        }
    }

    [TestMethod]
    public async Task StripSensitiveDropsTheArtistAndTheWindowsCommentAndAuthor()
    {
        static ExifValue Utf16(string text)
        {
            var bytes = System.Text.Encoding.Unicode.GetBytes(text + "\0");
            return new ExifValue(ExifType.Byte, bytes.Length, bytes);
        }
        var primary = new ExifDirectory()
            .With(ExifTag.Make, ExifValue.FromString("TestCam"))
            .With(ExifTag.Copyright, ExifValue.FromString("(c) Studio"))
            .With(ExifTag.Artist, ExifValue.FromString("Sam Rivera"))
            .With(ExifTag.WindowsComment, Utf16("private note"))
            .With(ExifTag.WindowsAuthor, Utf16("Jane Doe"));
        var exif = new ExifData(primary, null, null, null, default);

        var stripped = exif.StripSensitive();
        foreach (var tag in new[] { ExifTag.Artist, ExifTag.WindowsComment, ExifTag.WindowsAuthor })
            Assert.IsFalse(stripped.Primary.Contains(tag), $"tag 0x{tag:X4} survived StripSensitive");
        Assert.AreEqual("TestCam", stripped.Make);
        Assert.AreEqual("(c) Studio", stripped.Copyright);

        var secrets = new[]
        {
            System.Text.Encoding.ASCII.GetBytes("Sam Rivera"),
            System.Text.Encoding.Unicode.GetBytes("private note"),
            System.Text.Encoding.Unicode.GetBytes("Jane Doe"),
        };
        var processor = new ImageProcessor();
        using var img = AdversarialGeometryTests.Noise(32, 32, 19);
        img.Metadata = new ImageMetadata(exif, sourceFormat: ImageFormat.Jpeg);
        foreach (var format in new[] { ImageFormat.Jpeg, ImageFormat.Png })
        {
            var preserved = await processor.EncodeAsync(img, new ImageExportOptions { Format = format, Metadata = MetadataPolicy.Preserve });
            var encoded = await processor.EncodeAsync(img, new ImageExportOptions { Format = format, Metadata = MetadataPolicy.StripSensitive });
            foreach (var secret in secrets)
            {
                Assert.IsGreaterThanOrEqualTo(0, preserved.Data.AsSpan().IndexOf(secret), $"{format} dropped a secret even when preserving");
                Assert.AreEqual(-1, encoded.Data.AsSpan().IndexOf(secret), $"{format} kept a secret");
            }
            using var reread = await processor.DecodeAsync(encoded.Data);
            Assert.AreEqual("TestCam", reread.Metadata?.Exif?.Make);
            Assert.AreEqual("(c) Studio", reread.Metadata?.Exif?.Copyright);
        }
    }

    [TestMethod]
    public void EntriesSharingOneLargeValueAreCopiedOnlyUpToABudget()
    {
        const int entries = 300;
        const int valueBytes = 1 << 20;
        var valueOffset = 8 + 2 + entries * 12 + 4;
        var tiff = new byte[valueOffset + valueBytes];
        "II*\0"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(8), entries);
        for (var i = 0; i < entries; i++)
        {
            var entry = tiff.AsSpan(10 + i * 12);
            BinaryPrimitives.WriteUInt16LittleEndian(entry, (ushort)(0x1000 + i));
            BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], (ushort)ExifType.Undefined);
            BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], valueBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)valueOffset);
        }

        var exif = ExifData.Parse(tiff);

        Assert.IsNotNull(exif);
        Assert.IsGreaterThan(0, exif.Primary.Count);
        Assert.IsLessThan(entries, exif.Primary.Count);
        Assert.IsLessThanOrEqualTo(4L << 20, exif.Primary.Tags.Sum(t => (long)t.Value.Data.Length));
    }

    [TestMethod]
    public void ValuesAcrossDirectoriesAndTheThumbnailShareTheBudget()
    {
        const int valueBytes = 1 << 20;
        const int exifOffset = 8 + 2 + 4 * 12 + 4;
        const int ifd1Offset = exifOffset + 2 + 3 * 12 + 4;
        const int valueOffset = ifd1Offset + 2 + 2 * 12 + 4;
        var tiff = new byte[valueOffset + 2 * valueBytes];
        "II*\0"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), 8);
        void WriteDirectory(int at, int next, params (ushort Tag, ExifType Type, uint Count, uint Value)[] entries)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(at), (ushort)entries.Length);
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = tiff.AsSpan(at + 2 + i * 12);
                BinaryPrimitives.WriteUInt16LittleEndian(entry, entries[i].Tag);
                BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], (ushort)entries[i].Type);
                BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], entries[i].Count);
                BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], entries[i].Value);
            }
            BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(at + 2 + entries.Length * 12), (uint)next);
        }
        WriteDirectory(8, ifd1Offset,
            (ExifTag.ExifIfdPointer, ExifType.Long, 1, exifOffset),
            (0x1001, ExifType.Undefined, valueBytes, valueOffset),
            (0x1002, ExifType.Undefined, valueBytes, valueOffset),
            (0x1003, ExifType.Undefined, valueBytes, valueOffset));
        WriteDirectory(exifOffset, 0,
            (0x2001, ExifType.Undefined, valueBytes, valueOffset),
            (0x2002, ExifType.Undefined, valueBytes, valueOffset),
            (0x2003, ExifType.Undefined, valueBytes, valueOffset));
        WriteDirectory(ifd1Offset, 0,
            (0x0201, ExifType.Long, 1, valueOffset),
            (0x0202, ExifType.Long, 1, 2 * valueBytes));

        var exif = ExifData.Parse(tiff);

        Assert.IsNotNull(exif);
        Assert.AreEqual(3, exif.Primary.Count);
        var kept = exif.Primary.Tags.Concat(exif.Exif.Tags).Sum(t => (long)t.Value.Data.Length) + exif.Thumbnail.Length;
        Assert.IsLessThanOrEqualTo(4L << 20, kept);
    }

    /// <summary>
    /// A tag listed many times keeps only its last value, so only that one is charged to the budget: the copies cannot
    /// spend it and leave the orientation after them unread.
    /// </summary>
    [TestMethod]
    public void ATagRepeatedPastTheBudgetLeavesTheOtherTagsReadable()
    {
        const int copies = 300;
        const int valueBytes = 1 << 20;
        var valueOffset = 8 + 2 + (copies + 1) * 12 + 4;
        var tiff = new byte[valueOffset + valueBytes + 1];
        "II*\0"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(8), copies + 1);
        for (var i = 0; i < copies; i++)
        {
            var entry = tiff.AsSpan(10 + i * 12);
            BinaryPrimitives.WriteUInt16LittleEndian(entry, 0x1000);
            BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], (ushort)ExifType.Undefined);
            // The last copy is one byte longer, so which one was kept shows.
            BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], (uint)(i == copies - 1 ? valueBytes + 1 : valueBytes));
            BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)valueOffset);
        }
        var orientation = tiff.AsSpan(10 + copies * 12);
        BinaryPrimitives.WriteUInt16LittleEndian(orientation, ExifTag.Orientation);
        BinaryPrimitives.WriteUInt16LittleEndian(orientation[2..], (ushort)ExifType.Short);
        BinaryPrimitives.WriteUInt32LittleEndian(orientation[4..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(orientation[8..], (ushort)Orientation.Rotate90);

        var exif = ExifData.Parse(tiff);

        Assert.IsNotNull(exif);
        Assert.AreEqual(2, exif.Primary.Count);
        Assert.AreEqual(valueBytes + 1, exif.Primary.Get(0x1000)!.Data.Length);
        Assert.AreEqual(Orientation.Rotate90, exif.Orientation);
    }

    [TestMethod]
    public void AnIccProfileThatInflatesPastTheCapIsDropped()
    {
        using var img = AdversarialGeometryTests.Noise(8, 8, 3);
        var png = Codecs.Png.PngEncoder.Encode(img).Data;

        var bomb = PngContainer.WriteMetadata(png, new ImageMetadata(null, new byte[5 << 20], default, ImageFormat.Png));
        Assert.IsLessThan(64 * 1024, bomb.Length);
        Assert.IsTrue(PngContainer.ReadMetadata(bomb).IccProfile.IsEmpty);

        var profile = new byte[1 << 20];
        new Random(5).NextBytes(profile);
        var real = PngContainer.WriteMetadata(png, new ImageMetadata(null, profile, default, ImageFormat.Png));
        CollectionAssert.AreEqual(profile, PngContainer.ReadMetadata(real).IccProfile.ToArray());
    }

    /// <summary>
    /// The standard allows one iCCP chunk. A dropped profile leaves none read, so going by that, every further copy of a
    /// bomb would be inflated up to the cap in turn; only the first chunk is ever inflated.
    /// </summary>
    [TestMethod]
    public void OnlyTheFirstIccProfileIsRead()
    {
        using var img = AdversarialGeometryTests.Noise(8, 8, 3);
        var png = Codecs.Png.PngEncoder.Encode(img).Data;
        var bomb = PngContainer.WriteMetadata(png, new ImageMetadata(null, new byte[5 << 20], default, ImageFormat.Png));
        var profile = new byte[1024];
        new Random(9).NextBytes(profile);
        var real = PngContainer.WriteMetadata(png, new ImageMetadata(null, profile, default, ImageFormat.Png));
        var bombChunk = PngContainer.EnumerateChunks(bomb).Single(c => c.TypeName == "iCCP");
        var realChunk = PngContainer.EnumerateChunks(real).Single(c => c.TypeName == "iCCP");

        // The bomb, repeated a thousand times, then a valid profile, all before the image data.
        var ihdrEnd = 8 + 25;
        var repeated = png[..ihdrEnd]
            .Concat(Enumerable.Repeat(bomb.AsSpan(bombChunk.Offset, bombChunk.TotalLength).ToArray(), 1000).SelectMany(c => c))
            .Concat(real.AsSpan(realChunk.Offset, realChunk.TotalLength).ToArray())
            .Concat(png[ihdrEnd..])
            .ToArray();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var metadata = PngContainer.ReadMetadata(repeated);
        Assert.IsLessThan(2000, watch.ElapsedMilliseconds, $"Reading took {watch.ElapsedMilliseconds} ms.");
        Assert.IsTrue(metadata.IccProfile.IsEmpty);

        var first = png[..ihdrEnd].Concat(real.AsSpan(realChunk.Offset, realChunk.TotalLength).ToArray())
            .Concat(bomb.AsSpan(bombChunk.Offset, bombChunk.TotalLength).ToArray()).Concat(png[ihdrEnd..]).ToArray();
        CollectionAssert.AreEqual(profile, PngContainer.ReadMetadata(first).IccProfile.ToArray());
    }

    /// <summary>
    /// The reader keeps a tag in whichever directory a file put it, and the writer puts it back there, so the sensitive
    /// tags are stripped from every directory, not only the one the standard names.
    /// </summary>
    [TestMethod]
    public async Task StripSensitiveRemovesASensitiveTagFromWhicheverDirectoryHoldsIt()
    {
        var primary = new ExifDirectory()
            .With(ExifTag.Make, ExifValue.FromString("TestCam"))
            .With(ExifTag.OwnerName, ExifValue.FromString("Owner In Ifd0"))
            .With(ExifTag.BodySerialNumber, ExifValue.FromString("SN-IFD0-123"));
        var exifDirectory = new ExifDirectory()
            .With(ExifTag.Iso, ExifValue.FromShort(100))
            .With(ExifTag.Artist, ExifValue.FromString("Artist In ExifIfd"))
            .With(ExifTag.WindowsAuthor, ExifValue.FromString("Author In ExifIfd"));
        var exif = new ExifData(primary, exifDirectory, null, null, default);

        var stripped = exif.StripSensitive();
        foreach (var directory in new[] { stripped.Primary, stripped.Exif, stripped.Interop })
            foreach (var tag in new[] { ExifTag.OwnerName, ExifTag.BodySerialNumber, ExifTag.Artist, ExifTag.WindowsAuthor })
                Assert.IsFalse(directory.Contains(tag), $"tag 0x{tag:X4} survived StripSensitive");
        Assert.AreEqual("TestCam", stripped.Make);
        Assert.IsTrue(stripped.Exif.Contains(ExifTag.Iso));

        var processor = new ImageProcessor();
        using var img = AdversarialGeometryTests.Noise(16, 16, 23);
        img.Metadata = new ImageMetadata(exif, sourceFormat: ImageFormat.Jpeg);
        var encoded = await processor.EncodeAsync(img, new ImageExportOptions { Format = ImageFormat.Jpeg, Metadata = MetadataPolicy.StripSensitive });
        foreach (var secret in new[] { "Owner In Ifd0", "SN-IFD0-123", "Artist In ExifIfd", "Author In ExifIfd" })
            Assert.AreEqual(-1, encoded.Data.AsSpan().IndexOf(System.Text.Encoding.ASCII.GetBytes(secret)), $"kept {secret}");
    }

    [TestMethod]
    public async Task PreservePolicyKeepsDescriptiveMetadataThroughAJpegRoundTrip()
    {
        var processor = new ImageProcessor();
        using var img = AdversarialGeometryTests.Noise(32, 32, 13);
        img.Metadata = new ImageMetadata(BuildSensitiveExif(), sourceFormat: ImageFormat.Jpeg);

        var encoded = await processor.EncodeAsync(img, ImageExportOptions.Jpeg with { Metadata = MetadataPolicy.Preserve });
        using var decoded = await processor.DecodeAsync(encoded.Data);

        Assert.AreEqual("TestCam", decoded.Metadata?.Exif?.Primary.Get(ExifTag.Make)?.GetString());
        Assert.AreEqual("T1000", decoded.Metadata?.Exif?.Primary.Get(ExifTag.Model)?.GetString());
    }

    /// <summary>Exported metadata must describe the exported pixels, not the ones that went in.</summary>
    [TestMethod]
    public async Task ExportedMetadataDescribesTheExportedPixels()
    {
        var processor = new ImageProcessor();
        using var img = AdversarialGeometryTests.Noise(64, 48, 17);
        img.Metadata = new ImageMetadata(
            new ExifData(
                new ExifDirectory().With(ExifTag.Make, ExifValue.FromString("TestCam")),
                new ExifDirectory()
                    .With(ExifTag.PixelXDimension, ExifValue.FromLong(64))
                    .With(ExifTag.PixelYDimension, ExifValue.FromLong(48)),
                null, null, default),
            sourceFormat: ImageFormat.Jpeg);

        using var resized = new ResizeOperation(32, 24, ResizeMode.Stretch).Apply(img, OperationContext.Default);
        var encoded = await processor.EncodeAsync(resized, ImageExportOptions.Jpeg with { Metadata = MetadataPolicy.Preserve });
        using var decoded = await processor.DecodeAsync(encoded.Data);

        Assert.AreEqual(new Size(32, 24), decoded.Size);
        Assert.AreEqual(32, decoded.Metadata?.Exif?.Exif.Get(ExifTag.PixelXDimension)?.GetInt32());
        Assert.AreEqual(24, decoded.Metadata?.Exif?.Exif.Get(ExifTag.PixelYDimension)?.GetInt32());
    }

    /// <summary>An EXIF block must survive a write/read round trip unchanged, in the little-endian form written.</summary>
    [TestMethod]
    public void ExifWriteReadRoundTripPreservesValues()
    {
        var original = BuildSensitiveExif();
        var bytes = ExifWriter.Write(original);
        var parsed = ExifData.Parse(bytes);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("TestCam", parsed!.Primary.Get(ExifTag.Make)?.GetString());
        Assert.AreEqual("SN-12345", parsed.Exif.Get(ExifTag.BodySerialNumber)?.GetString());
        Assert.IsNotNull(parsed.Location);
        Assert.AreEqual(original.Location!.Value.Latitude, parsed.Location!.Value.Latitude, 0.5e-6);
        Assert.AreEqual(original.Location!.Value.Longitude, parsed.Location!.Value.Longitude, 0.5e-6);
    }
}
