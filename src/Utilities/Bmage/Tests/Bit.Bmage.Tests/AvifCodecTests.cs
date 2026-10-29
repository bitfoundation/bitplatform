using System.Buffers.Binary;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Avif;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Geometry;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Tests;

/// <summary>
/// The managed AVIF decoder. The assets were encoded by libaom through Magick.NET, and each PNG beside one is libheif's
/// decode of it. libheif repeats chroma samples rather than interpolating them, so the comparisons decode the same way;
/// AV1 reconstruction is exact, which leaves only the YUV to RGB rounding (at most one level) between the two.
/// </summary>
[TestClass]
public sealed class AvifCodecTests
{
    private static byte[] Asset(string name) => File.ReadAllBytes(Path.Combine("Assets", "Avif", name));

    private static ImageBuffer DecodeLikeLibheif(byte[] data, DecodeOptions? options = null)
        => AvifDecoder.Decode(data, options ?? DecodeOptions.Default, null, nearestChroma: true);

    [TestMethod]
    [DataRow("photo-420")]        // 4:2:0, quantizer matrices, CDEF, loop filter
    [DataRow("photo-422-12bit")]  // 4:2:2 at 12 bits
    [DataRow("photo-444-10bit")]  // 4:4:4 at 10 bits
    [DataRow("photo-lossless")]   // lossless: Walsh-Hadamard transform, identity matrix
    [DataRow("gray")]             // monochrome
    [DataRow("alpha")]            // an alpha auxiliary image
    [DataRow("screen")]           // screen content tools: palettes
    [DataRow("filters")]          // self-guided loop restoration, CDEF, delta q
    public void MatchesLibheifDecode(string name)
    {
        using var decoded = DecodeLikeLibheif(Asset(name + ".avif"));
        using var reference = PngDecoder.Decode(Asset(name + ".png"));
        Assert.AreEqual(reference.Width, decoded.Width);
        Assert.AreEqual(reference.Height, decoded.Height);
        Assert.IsLessThanOrEqualTo(1, TestImages.MaxChannelDifference(reference, decoded));
    }

    [TestMethod]
    [DataRow("tiles", 4200, 40, "B8D5CE8E9031F4B65D2B532A34482963EEBFF9EF286EF6CA4407CA1A28EBF92F")]        // two tile columns
    [DataRow("screen-sb128", 700, 500, "930BE61667840BC187D73585F7A56B5F83606B5A0AF805D266889920EC0A183E")] // intra block copy, palettes, 128x128 superblocks (alpha)
    public void MatchesRecordedDecode(string name, int width, int height, string sha256)
    {
        // Recorded once these images were seen to match libheif within one level, as the others are checked above.
        using var decoded = DecodeLikeLibheif(Asset(name + ".avif"));
        Assert.AreEqual(width, decoded.Width);
        Assert.AreEqual(height, decoded.Height);
        Assert.AreEqual(sha256, Convert.ToHexString(SHA256.HashData(decoded.Bytes)));
    }

    [TestMethod]
    public void InterpolatesSubsampledChroma()
    {
        // The default decode interpolates chroma where libheif repeats it: different at colour edges, close overall.
        using var interpolated = AvifDecoder.Decode(Asset("photo-420.avif"));
        using var repeated = DecodeLikeLibheif(Asset("photo-420.avif"));
        Assert.IsGreaterThan(1, TestImages.MaxChannelDifference(repeated, interpolated));
        Assert.IsLessThan(8.0, TestImages.MeanChannelDifference(repeated, interpolated));

        // Full-resolution chroma has nothing to interpolate.
        using var full = AvifDecoder.Decode(Asset("photo-444-10bit.avif"));
        using var fullRepeated = DecodeLikeLibheif(Asset("photo-444-10bit.avif"));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(fullRepeated, full));
    }

    [TestMethod]
    public void DecodesAlpha()
    {
        using var decoded = AvifDecoder.Decode(Asset("alpha.avif"));
        Assert.AreEqual(255, decoded[2, 2].A);
        Assert.AreEqual(0, decoded[48, 32].A);
        Assert.AreEqual(69, decoded[2, 32].A);
        Assert.IsTrue(decoded.HasTransparency());
    }

    [TestMethod]
    public void IdentifyReadsHeaderOnly()
    {
        var decoder = new AvifDecoder();
        var info = decoder.Identify(Asset("alpha.avif"));
        Assert.IsNotNull(info);
        Assert.AreEqual(96, info.Width);
        Assert.AreEqual(64, info.Height);
        Assert.AreEqual(ImageFormat.Avif, info.Format);
        Assert.IsTrue(info.HasAlpha);
        Assert.IsFalse(decoder.Identify(Asset("photo-420.avif"))!.HasAlpha);
        Assert.IsNull(decoder.Identify(Asset("photo-420.png")));
    }

    [TestMethod]
    public async Task DecodesThroughTheDecoderInterface()
    {
        IImageDecoder decoder = new AvifDecoder();
        CollectionAssert.AreEqual(new[] { ImageFormat.Avif }, decoder.Formats.ToArray());
        using var decoded = await decoder.DecodeAsync(Asset("photo-420.avif"), DecodeOptions.Default, Memory.PooledPixelAllocator.Shared);
        Assert.AreEqual(96, decoded.Width);
        Assert.AreEqual(ImageFormat.Avif, decoded.Metadata!.SourceFormat);
    }

    // ------------------------------------------------------------------ HEIF container features

    [TestMethod]
    [DataRow(1, -1, Orientation.Rotate270)]      // irot turns anti-clockwise
    [DataRow(2, -1, Orientation.Rotate180)]
    [DataRow(3, -1, Orientation.Rotate90)]
    [DataRow(-1, 0, Orientation.FlipVertical)]   // imir axis 0 exchanges top and bottom
    [DataRow(-1, 1, Orientation.FlipHorizontal)] // axis 1 exchanges left and right
    [DataRow(1, 0, Orientation.Transpose)]       // turn anti-clockwise, then exchange top and bottom
    public void AppliesRotationAndMirroring(int irot, int imir, Orientation expected)
    {
        var properties = new List<byte[]>();
        if (irot >= 0) properties.Add(Box("irot", [(byte)irot]));
        if (imir >= 0) properties.Add(Box("imir", [(byte)imir]));
        var file = Rebuild("photo-420.avif", properties);
        using var plain = DecodeLikeLibheif(Asset("photo-420.avif"));
        using var upright = new OrientationOperation(expected).Apply(plain, OperationContext.Default);

        using var decoded = DecodeLikeLibheif(file);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(upright, decoded));
        Assert.AreEqual(Orientation.Normal, decoded.Metadata!.Orientation);

        // Without auto-orientation the pixels stay as stored and the metadata says how to show them.
        using var stored = DecodeLikeLibheif(file, new DecodeOptions { AutoOrient = false });
        Assert.AreEqual(0, TestImages.MaxChannelDifference(plain, stored));
        Assert.AreEqual(expected, stored.Metadata!.Orientation);

        var info = new AvifDecoder().Identify(file)!;
        Assert.AreEqual(upright.Width, info.Width);
        Assert.AreEqual(upright.Height, info.Height);
    }

    [TestMethod]
    public void AppliesCleanAperture()
    {
        // 50x30 centred, then moved 7 left and 3.5 up: the left edge at (96 - 50) / 2 - 7 = 16, the top at 17 - 3.5 = 13.5,
        // which rounds to 14.
        var clap = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(clap, 50);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(8), 30);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(12), 1);
        BinaryPrimitives.WriteInt32BigEndian(clap.AsSpan(16), -7);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(20), 1);
        BinaryPrimitives.WriteInt32BigEndian(clap.AsSpan(24), -7);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(28), 2);
        var file = Rebuild("photo-420.avif", [Box("clap", clap)]);
        using var plain = DecodeLikeLibheif(Asset("photo-420.avif"));
        using var expected = plain.CopyRegion(new Rectangle(16, 14, 50, 30));

        using var decoded = DecodeLikeLibheif(file);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, decoded));
        var info = new AvifDecoder().Identify(file)!;
        Assert.AreEqual(50, info.Width);
        Assert.AreEqual(30, info.Height);
    }

    [TestMethod]
    public void IgnoresCleanApertureOutsideTheImage()
    {
        var clap = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(clap, 500);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(8), 30);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(28), 1);
        using var decoded = DecodeLikeLibheif(Rebuild("photo-420.avif", [Box("clap", clap)]));
        Assert.AreEqual(96, decoded.Width);
        Assert.AreEqual(64, decoded.Height);
    }

    [TestMethod]
    public void ComposesImageGrids()
    {
        // A 2x2 grid of the same 96x64 tile, cropped to 180x100 by the grid's output size.
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var tile = source.GetItem(1)!;
        var grid = new byte[8];
        grid[2] = 1; // rows - 1
        grid[3] = 1; // columns - 1
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(4), 180);
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(6), 100);
        var file = new AvifBuilder()
            .Add(new AvifBuilder.Item(1, "grid", grid, [Ispe(180, 100)], [("dimg", [2, 2, 2, 2])]))
            .Add(new AvifBuilder.Item(2, "av01", source.GetItemData(tile, int.MaxValue), CopyProperties(source, tile), []))
            .Build(primary: 1);
        using var plain = DecodeLikeLibheif(Asset("photo-420.avif"));

        using var decoded = DecodeLikeLibheif(file);
        Assert.AreEqual(180, decoded.Width);
        Assert.AreEqual(100, decoded.Height);
        using var topLeft = decoded.CopyRegion(new Rectangle(0, 0, 96, 64));
        using var bottomRight = decoded.CopyRegion(new Rectangle(96, 64, 84, 36));
        using var tileCorner = plain.CopyRegion(new Rectangle(0, 0, 84, 36));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(plain, topLeft));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(tileCorner, bottomRight));
    }

    [TestMethod]
    public void RejectsGridsWhoseTilesDoNotFit()
    {
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var tile = source.GetItem(1)!;
        var grid = new byte[8];
        grid[3] = 1; // two columns of 96 for a 90-pixel-wide image: the second would lie wholly outside
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(4), 90);
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(6), 64);
        var file = new AvifBuilder()
            .Add(new AvifBuilder.Item(1, "grid", grid, [Ispe(90, 64)], [("dimg", [2, 2])]))
            .Add(new AvifBuilder.Item(2, "av01", source.GetItemData(tile, int.MaxValue), CopyProperties(source, tile), []))
            .Build(primary: 1);
        Assert.ThrowsExactly<ImageDecodeException>(() => AvifDecoder.Decode(file));
    }

    [TestMethod]
    public void UndoesPremultipliedAlpha()
    {
        var source = AvifContainer.Parse(Asset("alpha.avif"));
        var color = source.GetItem(1)!;
        var alpha = source.GetItem(2)!;
        var file = new AvifBuilder()
            .Add(new AvifBuilder.Item(1, "av01", source.GetItemData(color, int.MaxValue), CopyProperties(source, color), [("prem", [2])]))
            .Add(new AvifBuilder.Item(2, "av01", source.GetItemData(alpha, int.MaxValue), CopyProperties(source, alpha), [("auxl", [1])]))
            .Build(primary: 1);
        using var expected = DecodeLikeLibheif(Asset("alpha.avif"));
        AvifColor.Unpremultiply(expected);

        using var decoded = DecodeLikeLibheif(file);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, decoded));
    }

    [TestMethod]
    public void ReadsExifIccAndXmp()
    {
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var color = source.GetItem(1)!;
        var exif = new ExifData().WithPrimary(new ExifDirectory().With(ExifTag.Make, ExifValue.FromString("Bmage"))).WithOrientation(Orientation.Rotate90);
        var tiff = exif.ToTiff();
        var exifItem = new byte[4 + 6 + tiff.Length];
        BinaryPrimitives.WriteUInt32BigEndian(exifItem, 6); // exif_tiff_header_offset, past "Exif\0\0"
        "Exif\0\0"u8.CopyTo(exifItem.AsSpan(4));
        tiff.CopyTo(exifItem.AsSpan(10));
        var icc = Enumerable.Range(0, 200).Select(i => (byte)i).ToArray();
        var colr = new byte[4 + icc.Length];
        "prof"u8.CopyTo(colr);
        icc.CopyTo(colr.AsSpan(4));
        var xmp = Encoding.UTF8.GetBytes("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"/>");
        var file = new AvifBuilder()
            .Add(new AvifBuilder.Item(1, "av01", source.GetItemData(color, int.MaxValue), [.. CopyProperties(source, color), Box("colr", colr)], []))
            .Add(new AvifBuilder.Item(2, "Exif", exifItem, [], [("cdsc", [1])]))
            .Add(new AvifBuilder.Item(3, "mime", xmp, [], [("cdsc", [1])], "application/rdf+xml"))
            .Build(primary: 1);

        var metadata = AvifDecoder.ReadMetadata(file, "photo.avif");
        Assert.AreEqual("Bmage", metadata.Exif?.Make);
        CollectionAssert.AreEqual(icc, metadata.IccProfile.ToArray());
        CollectionAssert.AreEqual(xmp, metadata.Xmp.ToArray());
        Assert.AreEqual(ImageFormat.Avif, metadata.SourceFormat);
        Assert.AreEqual("photo.avif", metadata.FileName);

        // An Exif orientation does not turn AVIF pixels (irot and imir do), and the decoded image records them as upright.
        using var plain = DecodeLikeLibheif(Asset("photo-420.avif"));
        using var decoded = DecodeLikeLibheif(file);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(plain, decoded));
        Assert.AreEqual(Orientation.Normal, decoded.Metadata!.Orientation);
        Assert.AreEqual("Bmage", decoded.Metadata.Exif?.Make);

        using var withoutMetadata = DecodeLikeLibheif(file, new DecodeOptions { ReadMetadata = false });
        Assert.IsNull(withoutMetadata.Metadata!.Exif);
    }

    [TestMethod]
    public void DecodesTheFirstFrameOfAnImageSequence()
    {
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var color = source.GetItem(1)!;
        var file = AvifBuilder.Sequence(source.GetItemData(color, int.MaxValue), source.PropertyData(source.FindProperty(color, AvifContainer.Av1C)!.Value).ToArray(), 96, 64);
        using var plain = DecodeLikeLibheif(Asset("photo-420.avif"));

        using var decoded = DecodeLikeLibheif(file);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(plain, decoded));
        Assert.AreEqual(96, new AvifDecoder().Identify(file)!.Width);
    }

    // ------------------------------------------------------------------ limits and hostile input

    [TestMethod]
    public void RejectsImagesOverTheLimitsBeforeDecoding()
    {
        var limits = new ImageLimits { MaxWidth = 64, MaxHeight = 64 };
        Assert.ThrowsExactly<ImageLimitExceededException>(() => AvifDecoder.Decode(Asset("photo-420.avif"), new DecodeOptions { Limits = limits }));
        Assert.ThrowsExactly<ImageLimitExceededException>(() => AvifDecoder.Decode(Asset("photo-420.avif"), new DecodeOptions { Limits = ImageLimits.Default with { MaxEncodedBytes = 100 } }));

        // A header claiming 100000x100000 is refused from the header, not by running out of memory.
        var huge = Rebuild("photo-420.avif", [], Ispe(100_000, 100_000));
        Assert.ThrowsExactly<ImageLimitExceededException>(() => AvifDecoder.Decode(huge));
        Assert.AreEqual(100_000, new AvifDecoder().Identify(huge)!.Width);
    }

    [TestMethod]
    public void RejectsDataThatIsNotAvif()
    {
        Assert.ThrowsExactly<ImageDecodeException>(() => AvifDecoder.Decode(Asset("photo-420.png")));
        Assert.ThrowsExactly<ImageDecodeException>(() => AvifDecoder.Decode([]));
    }

    [TestMethod]
    public void RejectsItemsPointingOutsideTheFile()
    {
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var item = source.GetItem(1)!;
        var file = new AvifBuilder { DataOffsetShift = 1_000_000 }
            .Add(new AvifBuilder.Item(1, "av01", source.GetItemData(item, int.MaxValue), CopyProperties(source, item), []))
            .Build(primary: 1);
        Assert.ThrowsExactly<ImageDecodeException>(() => AvifDecoder.Decode(file));
    }

    [TestMethod]
    [DataRow("photo-420")]
    [DataRow("alpha")]
    [DataRow("filters")]
    [DataRow("screen-sb128")]
    public void TruncatedFilesFailCleanly(string name)
    {
        var data = Asset(name + ".avif");
        for (var length = 0; length < data.Length; length += 1 + length / 8)
            AssertFailsCleanly(data[..length]);
    }

    [TestMethod]
    public void CorruptedFilesFailCleanly()
    {
        var random = new Random(14333);
        foreach (var name in new[] { "photo-420", "alpha", "photo-422-12bit", "screen", "screen-sb128", "tiles" })
        {
            var data = Asset(name + ".avif");
            for (var i = 0; i < 60; i++)
            {
                var corrupt = data.ToArray();
                for (var k = 0; k < 1 + random.Next(6); k++)
                {
                    // Half the damage goes into the headers, where it reaches the most code.
                    var at = random.Next(2) == 0 ? random.Next(Math.Min(corrupt.Length, 400)) : random.Next(corrupt.Length);
                    corrupt[at] = (byte)random.Next(256);
                }
                AssertFailsCleanly(corrupt);
            }
        }
    }

    [TestMethod]
    public void HonoursCancellation()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => AvifDecoder.Decode(Asset("filters.avif"), cancellationToken: cancelled.Token));
    }

    [TestMethod]
    [DataRow(1, 13, false)]  // BT.709 primaries, sRGB transfer
    [DataRow(2, 2, false)]   // unspecified
    [DataRow(12, 13, true)]  // Display P3
    [DataRow(9, 16, true)]   // BT.2020 with PQ: HDR
    [DataRow(1, 18, true)]   // HLG
    public void TellsWhenItsColoursAreNotSrgb(int primaries, int transfer, bool expected)
    {
        var nclx = new byte[11];
        "nclx"u8.CopyTo(nclx);
        BinaryPrimitives.WriteUInt16BigEndian(nclx.AsSpan(4), (ushort)primaries);
        BinaryPrimitives.WriteUInt16BigEndian(nclx.AsSpan(6), (ushort)transfer);
        BinaryPrimitives.WriteUInt16BigEndian(nclx.AsSpan(8), 1);
        var file = Rebuild("photo-420.avif", [Box("colr", nclx)]);

        Assert.AreEqual(expected, AvifDecoder.HasNonSrgbColors(file));
        Assert.IsFalse(AvifDecoder.HasNonSrgbColors(Asset("photo-420.avif")), "no colr says nothing about the gamut");
        // The colours are decoded either way; only a host with a browser can do better with them.
        using var decoded = AvifDecoder.Decode(file);
        Assert.AreEqual(ImageFormat.Avif, decoded.Metadata!.SourceFormat);
    }

    private static void AssertFailsCleanly(byte[] data)
    {
        try
        {
            new AvifDecoder().Identify(data);
            using var image = AvifDecoder.Decode(data);
            Assert.IsGreaterThan(0, image.Width);
        }
        catch (ImageDecodeException)
        {
        }
        catch (ImageLimitExceededException)
        {
        }
    }

    // ------------------------------------------------------------------ building files

    private static byte[] Box(string type, ReadOnlySpan<byte> payload)
    {
        var box = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(box, (uint)box.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(box, 4);
        payload.CopyTo(box.AsSpan(8));
        return box;
    }

    private static byte[] Ispe(int width, int height)
    {
        var payload = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8), (uint)height);
        return Box("ispe", payload);
    }

    private static List<byte[]> CopyProperties(AvifContainer file, HeifItem item)
        => item.Properties.Select(p => Box(FourCc(p.Type), file.PropertyData(p))).ToList();

    private static string FourCc(uint type) => Encoding.ASCII.GetString([(byte)(type >> 24), (byte)(type >> 16), (byte)(type >> 8), (byte)type]);

    /// <summary>The asset's primary item with extra properties, and optionally its ispe replaced.</summary>
    private static byte[] Rebuild(string asset, List<byte[]> extraProperties, byte[]? ispe = null)
    {
        var source = AvifContainer.Parse(Asset(asset));
        var item = source.GetItem(1)!;
        var properties = CopyProperties(source, item);
        if (ispe is not null) properties = [.. properties.Where(p => Encoding.ASCII.GetString(p, 4, 4) != "ispe"), ispe];
        return new AvifBuilder()
            .Add(new AvifBuilder.Item(1, "av01", source.GetItemData(item, int.MaxValue), [.. properties, .. extraProperties], []))
            .Build(primary: 1);
    }

    /// <summary>Writes minimal HEIF files: ftyp, then meta (hdlr, pitm, iinf, iloc, iref, iprp) and an mdat.</summary>
    private sealed class AvifBuilder
    {
        public sealed record Item(uint Id, string Type, byte[] Data, List<byte[]> Properties, List<(string Type, uint[] To)> References, string? ContentType = null);

        private readonly List<Item> _items = [];

        /// <summary>Added to every iloc offset, to point items somewhere they are not.</summary>
        public int DataOffsetShift { get; init; }

        public AvifBuilder Add(Item item)
        {
            _items.Add(item);
            return this;
        }

        public byte[] Build(uint primary)
        {
            var ftyp = Box("ftyp", [.. "avif"u8, 0, 0, 0, 0, .. "mif1"u8, .. "avif"u8, .. "miaf"u8]);
            // iloc offsets are 32-bit fields, so the meta box is the same size whatever they hold: build it once to learn
            // where the mdat payload starts, then again with the real offsets.
            var meta = Meta(primary, 0);
            meta = Meta(primary, ftyp.Length + meta.Length + 8);
            var mdat = Box("mdat", _items.SelectMany(i => i.Data).ToArray());
            return [.. ftyp, .. meta, .. mdat];
        }

        private byte[] Meta(uint primary, int dataStart)
        {
            var hdlr = Box("hdlr", [0, 0, 0, 0, 0, 0, 0, 0, .. "pict"u8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
            var pitm = Box("pitm", [0, 0, 0, 0, (byte)(primary >> 8), (byte)primary]);
            var infe = _items.Select(i =>
            {
                var payload = new List<byte> { 2, 0, 0, 0, (byte)(i.Id >> 8), (byte)i.Id, 0, 0 };
                payload.AddRange(Encoding.ASCII.GetBytes(i.Type));
                payload.Add(0); // item_name
                if (i.ContentType is not null) payload.AddRange([.. Encoding.ASCII.GetBytes(i.ContentType), 0]);
                return Box("infe", payload.ToArray());
            }).ToList();
            var iinf = Box("iinf", [0, 0, 0, 0, 0, (byte)_items.Count, .. infe.SelectMany(b => b)]);
            var iloc = new List<byte> { 0, 0, 0, 0, 0x44, 0x00, 0, (byte)_items.Count };
            var offset = dataStart + DataOffsetShift;
            foreach (var i in _items)
            {
                iloc.AddRange([(byte)(i.Id >> 8), (byte)i.Id, 0, 0, 0, 1]);
                iloc.AddRange(BigEndian((uint)offset));
                iloc.AddRange(BigEndian((uint)i.Data.Length));
                offset += i.Data.Length;
            }
            var refs = _items.SelectMany(i => i.References.Select(r =>
                Box(r.Type, [(byte)(i.Id >> 8), (byte)i.Id, (byte)(r.To.Length >> 8), (byte)r.To.Length, .. r.To.SelectMany(t => new[] { (byte)(t >> 8), (byte)t })]))).ToList();
            var properties = _items.SelectMany(i => i.Properties).ToList();
            var ipco = Box("ipco", properties.SelectMany(p => p).ToArray());
            var ipma = new List<byte> { 0, 0, 0, 0 };
            ipma.AddRange(BigEndian((uint)_items.Count));
            var index = 1;
            foreach (var i in _items)
            {
                ipma.AddRange([(byte)(i.Id >> 8), (byte)i.Id, (byte)i.Properties.Count]);
                foreach (var _ in i.Properties) ipma.Add((byte)index++);
            }
            var iprp = Box("iprp", [.. ipco, .. Box("ipma", ipma.ToArray())]);
            var children = new List<byte>();
            children.AddRange(hdlr);
            children.AddRange(pitm);
            children.AddRange(iinf);
            children.AddRange(Box("iloc", iloc.ToArray()));
            if (refs.Count > 0) children.AddRange(Box("iref", [0, 0, 0, 0, .. refs.SelectMany(b => b)]));
            children.AddRange(iprp);
            return Box("meta", [0, 0, 0, 0, .. children]);
        }

        /// <summary>An 'avis' file with one video track of one sample and no meta box.</summary>
        public static byte[] Sequence(byte[] sample, byte[] av1C, int width, int height)
        {
            var ftyp = Box("ftyp", [.. "avis"u8, 0, 0, 0, 0, .. "avis"u8, .. "msf1"u8, .. "miaf"u8]);
            byte[] Moov(int sampleOffset)
            {
                var visualEntry = new byte[78];
                BinaryPrimitives.WriteUInt16BigEndian(visualEntry.AsSpan(6), 1); // data_reference_index
                BinaryPrimitives.WriteUInt16BigEndian(visualEntry.AsSpan(24), (ushort)width);
                BinaryPrimitives.WriteUInt16BigEndian(visualEntry.AsSpan(26), (ushort)height);
                var av01 = Box("av01", [.. visualEntry, .. Box("av1C", av1C)]);
                var stsd = Box("stsd", [0, 0, 0, 0, 0, 0, 0, 1, .. av01]);
                var stsz = Box("stsz", [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, .. BigEndian((uint)sample.Length)]);
                var stsc = Box("stsc", [0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1]);
                var stco = Box("stco", [0, 0, 0, 0, 0, 0, 0, 1, .. BigEndian((uint)sampleOffset)]);
                var stbl = Box("stbl", [.. stsd, .. stsz, .. stsc, .. stco]);
                return Box("moov", Box("trak", Box("mdia", Box("minf", stbl))));
            }
            var moov = Moov(0);
            moov = Moov(ftyp.Length + moov.Length + 8);
            return [.. ftyp, .. moov, .. Box("mdat", sample)];
        }

        private static byte[] BigEndian(uint value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            return bytes;
        }
    }
}
