namespace Bit.Bmage.Tests;

[TestClass]
public sealed class ExifTests
{
    /// <summary>Builds EXIF data resembling what a phone camera writes, including GPS.</summary>
    private static BmageExifData BuildCameraExif() => new BmageExifData()
        .WithPrimary(new BmageExifDirectory()
            .With(BmageExifTag.Make, BmageExifValue.FromString("BlazorPhone"))
            .With(BmageExifTag.Model, BmageExifValue.FromString("BP-9 Pro"))
            .With(BmageExifTag.Software, BmageExifValue.FromString("BP OS 3.1"))
            .With(BmageExifTag.Copyright, BmageExifValue.FromString("© 2026 Example"))
            .With(BmageExifTag.Orientation, BmageExifValue.FromShort(6)))
        .WithExif(new BmageExifDirectory()
            .With(BmageExifTag.DateTimeOriginal, BmageExifValue.FromString("2026:04:17 09:31:44"))
            .With(BmageExifTag.ExposureTime, BmageExifValue.FromRational((1, 250)))
            .With(BmageExifTag.FNumber, BmageExifValue.FromRational((18, 10)))
            .With(BmageExifTag.Iso, BmageExifValue.FromShort(200))
            .With(BmageExifTag.FocalLength, BmageExifValue.FromRational((26, 1)))
            .With(BmageExifTag.LensModel, BmageExifValue.FromString("BP 26mm f/1.8"))
            .With(BmageExifTag.BodySerialNumber, BmageExifValue.FromString("SN-4417-XY"))
            .With(BmageExifTag.UserComment, BmageExifValue.FromString("private note"))
            .With(BmageExifTag.PixelXDimension, BmageExifValue.FromLong(4032))
            .With(BmageExifTag.PixelYDimension, BmageExifValue.FromLong(3024)))
        .WithGps(new BmageExifDirectory()
            .With(BmageExifTag.GpsLatitudeRef, BmageExifValue.FromString("N"))
            .With(BmageExifTag.GpsLatitude, BmageExifValue.FromRational((51, 1), (30, 1), (2613, 100)))
            .With(BmageExifTag.GpsLongitudeRef, BmageExifValue.FromString("W"))
            .With(BmageExifTag.GpsLongitude, BmageExifValue.FromRational((0, 1), (7, 1), (3924, 100)))
            .With(BmageExifTag.GpsAltitudeRef, BmageExifValue.FromShort(0))
            .With(BmageExifTag.GpsAltitude, BmageExifValue.FromRational((35, 1))));

    [TestMethod]
    public void RoundTripsThroughTiffSerialisation()
    {
        var original = BuildCameraExif();
        var restored = BmageExifData.Parse(original.ToTiff());
        Assert.IsNotNull(restored);
        Assert.AreEqual("BlazorPhone", restored!.Make);
        Assert.AreEqual("BP-9 Pro", restored.Model);
        Assert.AreEqual("© 2026 Example", restored.Copyright);
        Assert.AreEqual(BmageOrientation.Rotate90, restored.Orientation);
        Assert.AreEqual(200, restored.Iso);
        Assert.AreEqual(1.0 / 250, restored.ExposureTime!.Value, 0.5e-6);
        Assert.AreEqual(1.8, restored.FNumber!.Value, 0.5e-3);
        Assert.AreEqual(26, restored.FocalLength!.Value, 0.5e-3);
        Assert.AreEqual("BP 26mm f/1.8", restored.LensModel);
        Assert.AreEqual(4032, restored.PixelXDimension);
    }

    [TestMethod]
    public void DecodesGpsCoordinates()
    {
        var exif = BuildCameraExif();
        var location = exif.Location;
        Assert.IsNotNull(location);
        Assert.AreEqual(51.5072583, location!.Value.Latitude, 0.5e-4);
        // West is negative.
        Assert.AreEqual(-0.1275, location.Value.Longitude, 0.5e-3);
        Assert.AreEqual(35, location.Value.Altitude);
    }

    [TestMethod]
    public void ParsesDatesAndRejectsMalformedOnes()
    {
        Assert.AreEqual(new DateTimeOffset(2026, 4, 17, 9, 31, 44, TimeSpan.Zero), BuildCameraExif().DateTaken);
        Assert.IsNull(BmageExifData.ParseDate("not a date"));
        Assert.IsNull(BmageExifData.ParseDate("2026:13:45 99:99:99"));
        Assert.IsNull(BmageExifData.ParseDate(null));
        Assert.IsNull(BmageExifData.ParseDate("2026:02:30 10:00:00"));
    }

    [TestMethod]
    public void ParsesDateOffsets()
    {
        var exif = new BmageExifData().WithExif(new BmageExifDirectory()
            .With(BmageExifTag.DateTimeOriginal, BmageExifValue.FromString("2026:04:17 09:31:44"))
            .With(BmageExifTag.OffsetTimeOriginal, BmageExifValue.FromString("+02:00")));
        Assert.AreEqual(TimeSpan.FromHours(2), exif.DateTaken!.Value.Offset);
    }

    [TestMethod]
    public void StripSensitiveRemovesLocationAndIdentifiersButKeepsTheCamera()
    {
        var stripped = BuildCameraExif().StripSensitive();
        Assert.IsNull(stripped.Location);
        Assert.IsFalse(stripped.HasLocation);
        Assert.IsNull(stripped.Exif.Get(BmageExifTag.BodySerialNumber));
        Assert.IsNull(stripped.Exif.Get(BmageExifTag.UserComment));
        // Descriptive data survives, which is the point of this policy.
        Assert.AreEqual("BlazorPhone", stripped.Make);
        Assert.AreEqual("© 2026 Example", stripped.Copyright);
        Assert.AreEqual(200, stripped.Iso);
    }

    [TestMethod]
    public void OrientationCanBeReadAndReset()
    {
        var exif = BuildCameraExif();
        Assert.AreEqual(BmageOrientation.Rotate90, exif.Orientation);
        Assert.AreEqual(BmageOrientation.Normal, exif.WithOrientation(BmageOrientation.Normal).Orientation);
    }

    [TestMethod]
    public void HandlesTruncatedAndCorruptDataWithoutThrowing()
    {
        var valid = BuildCameraExif().ToTiff();
        for (var length = 1; length < valid.Length; length += Math.Max(1, valid.Length / 40))
        {
            var truncated = valid[..length];
            // Must never throw, however mangled the input.
            var result = BmageExifData.Parse(truncated);
            _ = result?.Make;
            _ = result?.Location;
        }
        Assert.IsNull(BmageExifData.Parse([]));
        Assert.IsNull(BmageExifData.Parse([0xFF, 0xFF, 0xFF, 0xFF]));
    }

    [TestMethod]
    public void IgnoresAbsurdOffsetsRatherThanReadingOutOfBounds()
    {
        var data = BuildCameraExif().ToTiff();
        // Point the first IFD entry's value offset far beyond the buffer.
        var corrupted = (byte[])data.Clone();
        for (var i = 8 + 2 + 8; i + 4 <= corrupted.Length && i < 40; i++) corrupted[i] = 0xFE;
        var result = BmageExifData.Parse(corrupted);
        // Some fields may be lost, but parsing must complete safely.
        _ = result?.Make;
    }

    [TestMethod]
    public void MetadataPolicyControlsWhatSurvives()
    {
        var metadata = new BmageImageMetadata(BuildCameraExif(), new byte[] { 1, 2, 3, 4 }, "<x:xmpmeta/>"u8.ToArray(), BmageImageFormat.Jpeg);

        var removed = metadata.Apply(BmageMetadataPolicy.Remove, 100, 80);
        Assert.IsNull(removed.Exif);
        Assert.IsTrue(removed.IccProfile.IsEmpty);
        Assert.IsTrue(removed.Xmp.IsEmpty);

        var stripped = metadata.Apply(BmageMetadataPolicy.StripSensitive, 100, 80);
        Assert.IsNotNull(stripped.Exif);
        Assert.IsNull(stripped.Exif!.Location);
        Assert.AreEqual("BlazorPhone", stripped.Exif.Make);
        Assert.IsFalse(stripped.IccProfile.IsEmpty);
        // XMP can carry location too, so this policy drops it.
        Assert.IsTrue(stripped.Xmp.IsEmpty);
        Assert.AreEqual(100, stripped.Exif.PixelXDimension);

        var preserved = metadata.Apply(BmageMetadataPolicy.Preserve, 100, 80);
        Assert.IsNotNull(preserved.Exif!.Location);
        Assert.IsFalse(preserved.Xmp.IsEmpty);
        Assert.AreEqual(100, preserved.Exif.PixelXDimension);

        // The orientation tag describes the pixels in the buffer, so a policy carries it through rather than
        // forcing it to normal. Forcing it would export a sideways image whenever the caller decoded with
        // AutoOrient off, with nothing left in the file to correct it.
        Assert.AreEqual(BmageOrientation.Rotate90, stripped.Exif.Orientation);
        Assert.AreEqual(BmageOrientation.Rotate90, preserved.Exif.Orientation);

        // A buffer whose pixels have been brought upright reports normal, and that is what gets written.
        var upright = metadata.WithExif(metadata.Exif!.WithOrientation(BmageOrientation.Normal));
        Assert.AreEqual(BmageOrientation.Normal, upright.Apply(BmageMetadataPolicy.Preserve, 100, 80).Exif!.Orientation);
    }
}

[TestClass]
public sealed class ContainerTests
{
    [TestMethod]
    public void JpegMetadataSurvivesAWriteReadRoundTrip()
    {
        using var image = TestImages.Gradient(32, 32);
        var jpeg = BmageJpegEncoder.Encode(image, new BmageImageExportOptions { Format = BmageImageFormat.Jpeg, Quality = 0.9 }).Data;
        var metadata = new BmageImageMetadata(
            new BmageExifData().WithPrimary(new BmageExifDirectory().With(BmageExifTag.Artist, BmageExifValue.FromString("A Photographer"))),
            sourceFormat: BmageImageFormat.Jpeg);

        var withMetadata = BmageJpegContainer.WriteMetadata(jpeg, metadata);
        Assert.AreEqual(BmageImageFormat.Jpeg, BmageImageFormats.Detect(withMetadata));
        var read = BmageJpegContainer.ReadMetadata(withMetadata);
        Assert.AreEqual("A Photographer", read.Exif?.Artist);

        // The image must still decode after metadata surgery.
        using var decoded = BmageJpegDecoder.Decode(withMetadata);
        Assert.AreEqual(32, decoded.Width);
    }

    [TestMethod]
    public void WritingJpegMetadataTwiceDoesNotAccumulateSegments()
    {
        using var image = TestImages.Gradient(16, 16);
        var jpeg = BmageJpegEncoder.Encode(image).Data;
        var metadata = new BmageImageMetadata(new BmageExifData().WithPrimary(new BmageExifDirectory().With(BmageExifTag.Make, BmageExifValue.FromString("One"))));
        var once = BmageJpegContainer.WriteMetadata(jpeg, metadata);
        var twice = BmageJpegContainer.WriteMetadata(once, metadata);
        // The second write replaces the first rather than appending, so sizes match closely.
        Assert.AreEqual(once.Length, twice.Length);
        Assert.AreEqual("One", BmageJpegContainer.ReadMetadata(twice).Exif?.Make);
    }

    [TestMethod]
    public void PngMetadataSurvivesAWriteReadRoundTrip()
    {
        using var image = TestImages.Gradient(24, 24);
        var png = BmagePngEncoder.Encode(image).Data;
        var icc = new byte[] { 10, 20, 30, 40, 50 };
        var metadata = new BmageImageMetadata(
            new BmageExifData().WithPrimary(new BmageExifDirectory().With(BmageExifTag.Software, BmageExifValue.FromString("Bmage"))),
            icc,
            "<x:xmpmeta xmlns:x='adobe:ns:meta/'/>"u8.ToArray(),
            BmageImageFormat.Png);

        var withMetadata = BmagePngContainer.WriteMetadata(png, metadata);
        var read = BmagePngContainer.ReadMetadata(withMetadata);
        Assert.AreEqual("Bmage", read.Exif?.Software);
        Assert.AreSequenceEqual(icc, read.IccProfile.ToArray());
        Assert.Contains("xmpmeta", System.Text.Encoding.UTF8.GetString(read.Xmp.Span), StringComparison.Ordinal);

        using var decoded = BmagePngDecoder.Decode(withMetadata);
        Assert.AreEqual(24, decoded.Width);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(image, decoded));
    }

    [TestMethod]
    public void RejectsNonImageDataClearly()
    {
        var garbage = new byte[64];
        Assert.ThrowsExactly<BmageImageEncodeException>(() => BmageJpegContainer.WriteMetadata(garbage, BmageImageMetadata.Empty));
        Assert.ThrowsExactly<BmageImageEncodeException>(() => BmagePngContainer.WriteMetadata(garbage, BmageImageMetadata.Empty));
    }

    [TestMethod]
    public void ReadsWebPDimensionsFromEachHeaderKind()
    {
        // A minimal lossy WebP header: RIFF....WEBPVP8 ....<frame header with 64x48>
        var webp = new byte[] {
            (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0x20, 0, 0, 0,
            (byte)'W', (byte)'E', (byte)'B', (byte)'P',
            (byte)'V', (byte)'P', (byte)'8', (byte)' ', 0x10, 0, 0, 0,
            0, 0, 0, 0x9D, 0x01, 0x2A, 64, 0, 48, 0, 0, 0, 0, 0, 0, 0,
        };
        var size = BmageWebPContainer.ReadSize(webp);
        Assert.IsNotNull(size);
        Assert.AreEqual(64, size!.Value.Width);
        Assert.AreEqual(48, size.Value.Height);
    }
}

[TestClass]
public sealed class OrientationDecodeTests
{
    [TestMethod]
    [DataRow(BmageOrientation.Rotate90)]
    [DataRow(BmageOrientation.Rotate180)]
    [DataRow(BmageOrientation.Rotate270)]
    [DataRow(BmageOrientation.FlipHorizontal)]
    [DataRow(BmageOrientation.FlipVertical)]
    [DataRow(BmageOrientation.Transpose)]
    [DataRow(BmageOrientation.Transverse)]
    public void AutoOrientMakesEveryOrientationUpright(BmageOrientation orientation)
    {
        // Start from an upright image, store it rotated, and tag it so a decoder should undo the rotation.
        using var upright = TestImages.Quadrants(16, 16);
        using var stored = new BmageOrientationOperation(orientation.Inverse())
            .Apply(upright, BmageOperationContext.Default);

        var png = BmagePngEncoder.Encode(stored).Data;
        var tagged = BmagePngContainer.WriteMetadata(png, new BmageImageMetadata(BmageExifData.FromOrientation(orientation)));

        using var decoded = BmagePngDecoder.Decode(tagged, new BmageDecodeOptions { AutoOrient = true });
        Assert.AreEqual(upright.Size, decoded.Size);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(upright, decoded));
        Assert.AreEqual(BmageOrientation.Normal, decoded.Metadata!.Orientation);
    }

    [TestMethod]
    public void AutoOrientCanBeTurnedOff()
    {
        using var source = TestImages.Quadrants(8, 8);
        var png = BmagePngEncoder.Encode(source).Data;
        var tagged = BmagePngContainer.WriteMetadata(png, new BmageImageMetadata(BmageExifData.FromOrientation(BmageOrientation.Rotate90)));
        using var decoded = BmagePngDecoder.Decode(tagged, new BmageDecodeOptions { AutoOrient = false });
        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, decoded));
        // The orientation is reported rather than applied, so the caller can decide.
        Assert.AreEqual(BmageOrientation.Rotate90, decoded.Metadata!.Orientation);
    }
}
