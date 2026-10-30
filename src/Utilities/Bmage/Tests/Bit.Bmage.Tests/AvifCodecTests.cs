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
        // Without prem the same samples decode to the premultiplied colour; with it, each partly transparent pixel is
        // that colour divided by its alpha, worked out here rather than by the decoder's own helper.
        using var stored = DecodeLikeLibheif(Asset("alpha.avif"));
        static byte Straight(byte c, byte a) => (byte)Math.Min(255, Math.Round(c * 255.0 / a, MidpointRounding.AwayFromZero));

        using var decoded = DecodeLikeLibheif(file);
        var partial = 0;
        for (var y = 0; y < stored.Height; y++)
        {
            for (var x = 0; x < stored.Width; x++)
            {
                var p = stored[x, y];
                if (p.A is 0 or 255)
                {
                    Assert.AreEqual(p, decoded[x, y], $"({x}, {y})");
                    continue;
                }
                partial++;
                Assert.AreEqual(new Rgba32(Straight(p.R, p.A), Straight(p.G, p.A), Straight(p.B, p.A), p.A), decoded[x, y], $"({x}, {y})");
            }
        }
        Assert.IsGreaterThan(0, partial, "the asset has partly transparent pixels to undo");
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

    [TestMethod]
    [DataRow(-1, true)]  // no colr: the sequence header's BT.2020 PQ is what the decoder goes by
    [DataRow(2, true)]   // an nclx leaving everything unspecified says nothing either
    [DataRow(1, false)]  // an nclx that says BT.709 is believed over the bitstream
    public void TellsWhenTheSequenceHeaderSaysTheColoursAreNotSrgb(int nclxPrimaries, bool expected)
    {
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var item = source.GetItem(1)!;
        var obus = Obus(source.GetItemData(item, int.MaxValue));
        var stream = obus.SelectMany(o => Obu(o.Type, o.Type == 1 ? RewriteSequenceHeader(o.Payload, unreduce: false, color: (9, 16, 9)) : o.Payload)).ToArray();
        var properties = CopyProperties(source, item);
        if (nclxPrimaries >= 0)
        {
            var nclx = new byte[11];
            "nclx"u8.CopyTo(nclx);
            BinaryPrimitives.WriteUInt16BigEndian(nclx.AsSpan(4), (ushort)nclxPrimaries);
            BinaryPrimitives.WriteUInt16BigEndian(nclx.AsSpan(6), (ushort)(nclxPrimaries == 2 ? 2 : 13));
            BinaryPrimitives.WriteUInt16BigEndian(nclx.AsSpan(8), (ushort)(nclxPrimaries == 2 ? 2 : 1));
            properties.Add(Box("colr", nclx));
        }
        var file = new AvifBuilder().Add(new AvifBuilder.Item(1, "av01", stream, properties, [])).Build(primary: 1);

        Assert.AreEqual(expected, AvifDecoder.HasNonSrgbColors(file));
    }

    [TestMethod]
    public void ShowsAFrameAgainFromItsReferenceSlot()
    {
        using var plain = DecodeLikeLibheif(Asset("photo-420.avif"));

        using var decoded = DecodeLikeLibheif(ShownAgain(frameId: null, displayFrameId: null));
        Assert.AreEqual(0, TestImages.MaxChannelDifference(plain, decoded));
    }

    [TestMethod]
    public void ShowsAgainOnlyTheFrameTheStreamNames()
    {
        // A stream that numbers its frames names the one it shows again, and the slot must hold that one.
        using var plain = DecodeLikeLibheif(Asset("photo-420.avif"));
        using (var decoded = DecodeLikeLibheif(ShownAgain(frameId: 5, displayFrameId: 5)))
            Assert.AreEqual(0, TestImages.MaxChannelDifference(plain, decoded));

        Assert.ThrowsExactly<ImageDecodeException>(() => AvifDecoder.Decode(ShownAgain(frameId: 5, displayFrameId: 6)));
    }

    /// <summary>
    /// The still rewritten without its reduced header, then shown a second time with show_existing_frame, as an encoder
    /// may end a stream: a shown key frame fills every reference slot, so slot 0 holds it. With a <paramref name="frameId"/>
    /// the stream numbers its frames with 3 bit ids, and the frame is shown again by <paramref name="displayFrameId"/>.
    /// </summary>
    private static byte[] ShownAgain(int? frameId, int? displayFrameId)
    {
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var item = source.GetItem(1)!;
        var obus = Obus(source.GetItemData(item, int.MaxValue));
        var seqPayload = obus.First(o => o.Type == 1).Payload;
        var seq = Av1SequenceHeader.Parse(seqPayload);
        // show_existing_frame, frame_to_show_map_idx 0, display_frame_id when there is one, trailing bits.
        var showAgain = displayFrameId is { } id ? (byte)(0b1000_0001 | id << 1) : (byte)0b1000_1000;
        byte[] stream =
        [
            .. Obu(2, []),
            .. Obu(1, RewriteSequenceHeader(seqPayload, unreduce: true, color: null, frameIds: frameId is not null)),
            .. Obu(6, UnreduceFrame(obus.First(o => o.Type == 6).Payload, seq, frameId)),
            .. Obu(3, [showAgain]),
        ];
        return new AvifBuilder().Add(new AvifBuilder.Item(1, "av01", stream, CopyProperties(source, item), [])).Build(primary: 1);
    }

    [TestMethod]
    public void GridTileFailuresAreDecodeErrorsWhateverTheThreads()
    {
        // The tiles after the first decode side by side, each item once however many cells name it; one that is cut short
        // must still fail as the sequential loop would, as an ImageDecodeException a host can fall back on, not wrapped in
        // an AggregateException.
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var tile = source.GetItem(1)!;
        var data = source.GetItemData(tile, int.MaxValue);
        var grid = new byte[8];
        grid[2] = 1;
        grid[3] = 1;
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(4), 192);
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(6), 128);
        var file = new AvifBuilder()
            .Add(new AvifBuilder.Item(1, "grid", grid, [Ispe(192, 128)], [("dimg", [2, 4, 3, 3])]))
            .Add(new AvifBuilder.Item(2, "av01", data, CopyProperties(source, tile), []))
            .Add(new AvifBuilder.Item(3, "av01", data[..(data.Length / 2)], CopyProperties(source, tile), []))
            .Add(new AvifBuilder.Item(4, "av01", data, CopyProperties(source, tile), []))
            .Build(primary: 1);

        foreach (var threads in new[] { 1, 4 })
            Assert.ThrowsExactly<ImageDecodeException>(() => AvifDecoder.Decode(file, new DecodeOptions { MaxDegreeOfParallelism = threads }), $"{threads} threads");
    }

    [TestMethod]
    [DataRow(2, -1)] // turned half way round: no axes swap, but the clap still follows a transform
    [DataRow(-1, 0)] // mirrored
    public void IdentifyIgnoresACleanApertureAfterATransformAsDecodingDoes(int irot, int imir)
    {
        var clap = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(clap, 50);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(8), 30);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(28), 1);
        var properties = new List<byte[]>();
        if (irot >= 0) properties.Add(Box("irot", [(byte)irot]));
        if (imir >= 0) properties.Add(Box("imir", [(byte)imir]));
        properties.Add(Box("clap", clap));
        var file = Rebuild("photo-420.avif", properties);

        using var decoded = DecodeLikeLibheif(file);
        var info = new AvifDecoder().Identify(file)!;
        Assert.AreEqual((96, 64), (decoded.Width, decoded.Height));
        Assert.AreEqual((decoded.Width, decoded.Height), (info.Width, info.Height));
    }

    [TestMethod]
    public void IgnoresCleanAperturesWhoseEdgesOverflow()
    {
        // 1.5e9 wide, centred 1.75e9 to the right: the right edge is past int's range, which must not wrap into it.
        var clap = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(clap, 1_500_000_000);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(8), 30);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(12), 1);
        BinaryPrimitives.WriteInt32BigEndian(clap.AsSpan(16), 1_750_000_000);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt32BigEndian(clap.AsSpan(28), 1);
        var file = Rebuild("photo-420.avif", [Box("clap", clap)]);

        using var decoded = DecodeLikeLibheif(file);
        var info = new AvifDecoder().Identify(file)!;
        Assert.AreEqual((96, 64), (decoded.Width, decoded.Height));
        Assert.AreEqual((96, 64), (info.Width, info.Height));
    }

    [TestMethod]
    public void ReadsOnlyTheItemInfoEntriesTheCountGives()
    {
        // A grid and its tile, with iinf's entry_count cut to 1: the tile's infe that follows is not an entry, so the
        // tile has no type, as other parsers see it, and the grid is refused.
        var source = AvifContainer.Parse(Asset("photo-420.avif"));
        var tile = source.GetItem(1)!;
        var grid = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(4), 96);
        BinaryPrimitives.WriteUInt16BigEndian(grid.AsSpan(6), 64);
        var file = new AvifBuilder()
            .Add(new AvifBuilder.Item(1, "grid", grid, [Ispe(96, 64)], [("dimg", [2])]))
            .Add(new AvifBuilder.Item(2, "av01", source.GetItemData(tile, int.MaxValue), CopyProperties(source, tile), []))
            .Build(primary: 1);
        using (var whole = AvifDecoder.Decode(file)) Assert.AreEqual(96, whole.Width);

        var iinf = file.AsSpan().IndexOf("iinf"u8);
        Assert.AreEqual(2, BinaryPrimitives.ReadUInt16BigEndian(file.AsSpan(iinf + 8)));
        BinaryPrimitives.WriteUInt16BigEndian(file.AsSpan(iinf + 8), 1);
        Assert.ThrowsExactly<ImageDecodeException>(() => AvifDecoder.Decode(file));
    }

    [TestMethod]
    public async Task DecodesASliceOfALargerBufferInPlace()
    {
        // Through the decoder interface the file is read where it lies, so every offset is relative to the slice.
        var asset = Asset("alpha.avif");
        var padded = new byte[asset.Length + 64];
        asset.CopyTo(padded, 32);
        using var expected = AvifDecoder.Decode(asset);
        using var decoded = await new AvifDecoder().DecodeAsync(padded.AsMemory(32, asset.Length), DecodeOptions.Default, Memory.PooledPixelAllocator.Shared);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, decoded));
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

    // ------------------------------------------------------------------ rewriting AV1 streams

    /// <summary>The OBUs of an AV1 stream, each with its size field.</summary>
    private static List<(int Type, byte[] Payload)> Obus(byte[] data)
    {
        var obus = new List<(int, byte[])>();
        var pos = 0;
        while (pos < data.Length)
        {
            var header = data[pos];
            Assert.AreEqual(2, header & 6, "an OBU with a size field and no extension");
            pos += 1 + Av1BitReader.ReadLeb128(data, pos + 1, out var size);
            obus.Add(((header >> 3) & 15, data[pos..(pos + (int)size)]));
            pos += (int)size;
        }
        return obus;
    }

    private static byte[] Obu(int type, byte[] payload)
    {
        var size = new List<byte>();
        var v = payload.Length;
        do
        {
            size.Add((byte)((v & 0x7F) | (v > 0x7F ? 0x80 : 0)));
            v >>= 7;
        } while (v > 0);
        return [(byte)(type << 3 | 2), .. size, .. payload];
    }

    /// <summary>
    /// The assets' reduced still picture sequence header, rewritten bit by bit: in full, so frames may be shown again
    /// from reference slots, and, when <paramref name="color"/> is given, describing those colours.
    /// </summary>
    private static byte[] RewriteSequenceHeader(byte[] payload, bool unreduce, (int Primaries, int Transfer, int Matrix)? color, bool frameIds = false)
    {
        var seq = Av1SequenceHeader.Parse(payload);
        var b = new BitCopy(payload);
        var profile = b.Copy(3);
        b.Copy(1); // still_picture
        Assert.AreEqual(1, b.Skip(1), "the assets have reduced still picture headers");
        b.Write(unreduce ? 0 : 1, 1);
        var level = b.Skip(5);
        if (unreduce)
        {
            b.Write(0, 1);  // timing_info_present_flag
            b.Write(0, 1);  // initial_display_delay_present_flag
            b.Write(0, 5);  // operating_points_cnt_minus_1
            b.Write(0, 12); // operating_point_idc[0]
        }
        b.Write(level, 5);
        if (unreduce && level > 7) b.Write(0, 1); // seq_tier[0]
        b.Copy(8 + seq.FrameWidthBits + seq.FrameHeightBits);
        if (unreduce)
        {
            b.Write(frameIds ? 1 : 0, 1); // frame_id_numbers_present_flag
            if (frameIds) b.Write(0, 7); // delta_frame_id_length_minus_2 and additional_frame_id_length_minus_1: 3 bit ids
        }
        b.Copy(3); // use_128x128_superblock, enable_filter_intra, enable_intra_edge_filter
        if (unreduce)
        {
            b.Write(0, 5); // no inter-intra or masked compound, warped motion, dual filter or order hints
            b.Write(1, 1); // seq_choose_screen_content_tools, which the reduced header implies
            b.Write(1, 1); // seq_choose_integer_mv, likewise
        }
        b.Copy(3); // enable_superres, enable_cdef, enable_restoration
        var highBitdepth = b.Copy(1);
        if (profile == 2 && highBitdepth == 1) b.Copy(1); // twelve_bit
        if (profile != 1) Assert.AreEqual(0, b.Copy(1), "mono_chrome");
        if (color is { } c)
        {
            // Only the description changes: BT.2020 is not sRGB's special case, so the fields after it stay as they are.
            if (b.Skip(1) == 1) Assert.AreNotEqual((1, 13, 0), (b.Skip(8), b.Skip(8), b.Skip(8)));
            b.Write(1, 1);
            b.Write(c.Primaries, 8);
            b.Write(c.Transfer, 8);
            b.Write(c.Matrix, 8);
        }
        b.CopyTo(b.TrailingBit);
        return b.ToBytes(trailingBit: true);
    }

    /// <summary>A reduced still picture's frame OBU rewritten for the full sequence header <see cref="RewriteSequenceHeader"/> makes.</summary>
    private static byte[] UnreduceFrame(byte[] payload, Av1SequenceHeader reduced, int? frameId = null)
    {
        // The tile data starts at the byte after the header ends.
        var reader = new Av1BitReader(payload);
        var hdr = Av1FrameHeader.Parse(ref reader, reduced, 0, 0);
        var headerEnd = (int)reader.BitPosition;
        var b = new BitCopy(payload);
        b.Write(0, 1); // show_existing_frame
        b.Write(0, 2); // KEY_FRAME
        b.Write(1, 1); // show_frame
        var disableCdfUpdate = b.Copy(1);
        if (b.Copy(1) == 1) b.Copy(1); // allow_screen_content_tools, force_integer_mv
        if (frameId is { } id) b.Write(id, 3); // current_frame_id
        b.Write(0, 1); // frame_size_override_flag
        if (reduced.EnableSuperres && b.Copy(1) == 1) b.Copy(3);
        if (b.Copy(1) == 1) b.Copy(32); // render_size
        if (hdr.AllowScreenContentTools && hdr.UpscaledWidth == hdr.FrameWidth) b.Copy(1); // allow_intrabc
        if (disableCdfUpdate == 0) b.Write(1, 1); // disable_frame_end_update_cdf
        b.CopyTo(headerEnd);
        return [.. b.ToBytes(trailingBit: false), .. payload.AsSpan((headerEnd + 7) / 8)];
    }

    /// <summary>Copies bits, most significant first, from a source to an output, with some written in between.</summary>
    private sealed class BitCopy(byte[] source)
    {
        private readonly List<bool> _output = [];
        private int _pos;

        /// <summary>Where the source's trailing bits start: its last set bit.</summary>
        public int TrailingBit => Enumerable.Range(0, source.Length * 8).Last(Bit);

        private bool Bit(int i) => ((source[i >> 3] >> (7 - (i & 7))) & 1) != 0;

        public int Skip(int count)
        {
            var v = 0;
            for (var i = 0; i < count; i++) v = v << 1 | (Bit(_pos++) ? 1 : 0);
            return v;
        }

        public int Copy(int count)
        {
            var v = Skip(count);
            Write(v, count);
            return v;
        }

        public void CopyTo(int end)
        {
            while (_pos < end) Copy(1);
        }

        public void Write(int value, int count)
        {
            for (var i = count - 1; i >= 0; i--) _output.Add(((value >> i) & 1) != 0);
        }

        public byte[] ToBytes(bool trailingBit)
        {
            if (trailingBit) _output.Add(true);
            var bytes = new byte[(_output.Count + 7) / 8];
            for (var i = 0; i < _output.Count; i++)
                if (_output[i]) bytes[i >> 3] |= (byte)(0x80 >> (i & 7));
            return bytes;
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
