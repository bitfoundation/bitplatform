using System.Buffers.Binary;
using System.Text;

namespace Bit.Bmage;

/// <summary>
/// Parsed EXIF metadata: the primary (IFD0), EXIF, GPS and interoperability directories plus an optional embedded thumbnail.
/// Typed accessors are provided for common fields; every tag remains available through the directories.
/// </summary>
public sealed class BmageExifData
{
    public BmageExifData(BmageExifDirectory? primary = null, BmageExifDirectory? exif = null, BmageExifDirectory? gps = null, BmageExifDirectory? interop = null, ReadOnlyMemory<byte> thumbnail = default)
    {
        Primary = primary ?? new BmageExifDirectory();
        Exif = exif ?? new BmageExifDirectory();
        Gps = gps ?? new BmageExifDirectory();
        Interop = interop ?? new BmageExifDirectory();
        Thumbnail = thumbnail;
    }

    /// <summary>IFD0: camera make/model, orientation, software, dates, copyright.</summary>
    public BmageExifDirectory Primary { get; }
    /// <summary>The EXIF sub-IFD: exposure settings, original date, lens, pixel dimensions.</summary>
    public BmageExifDirectory Exif { get; }
    /// <summary>The GPS sub-IFD.</summary>
    public BmageExifDirectory Gps { get; }
    /// <summary>The interoperability sub-IFD.</summary>
    public BmageExifDirectory Interop { get; }
    /// <summary>Embedded JPEG thumbnail bytes from IFD1, or empty.</summary>
    public ReadOnlyMemory<byte> Thumbnail { get; }

    public bool IsEmpty => Primary.Count == 0 && Exif.Count == 0 && Gps.Count == 0 && Interop.Count == 0;

    // ---- Typed accessors ----

    public BmageOrientation Orientation
    {
        get
        {
            var v = Primary.Get(BmageExifTag.Orientation)?.GetInt32();
            return v is >= 1 and <= 8 ? (BmageOrientation)v : BmageOrientation.Normal;
        }
    }

    public string? Make => Primary.Get(BmageExifTag.Make)?.GetString();
    public string? Model => Primary.Get(BmageExifTag.Model)?.GetString();
    public string? Software => Primary.Get(BmageExifTag.Software)?.GetString();
    public string? Artist => Primary.Get(BmageExifTag.Artist)?.GetString();
    public string? Copyright => Primary.Get(BmageExifTag.Copyright)?.GetString();
    public string? ImageDescription => Primary.Get(BmageExifTag.ImageDescription)?.GetString();
    public string? LensModel => Exif.Get(BmageExifTag.LensModel)?.GetString();
    public string? UserComment => Exif.Get(BmageExifTag.UserComment)?.GetString();

    /// <summary>Exposure time in seconds.</summary>
    public double? ExposureTime => Exif.Get(BmageExifTag.ExposureTime)?.GetDouble();
    public double? FNumber => Exif.Get(BmageExifTag.FNumber)?.GetDouble();
    public int? Iso => Exif.Get(BmageExifTag.Iso)?.GetInt32();
    /// <summary>Focal length in millimetres.</summary>
    public double? FocalLength => Exif.Get(BmageExifTag.FocalLength)?.GetDouble();
    public int? FocalLengthIn35mmFilm => Exif.Get(BmageExifTag.FocalLengthIn35mmFilm)?.GetInt32();
    public double? ExposureBias => Exif.Get(BmageExifTag.ExposureBias)?.GetDouble();
    public bool? FlashFired => Exif.Get(BmageExifTag.Flash)?.GetInt32() is { } f ? (f & 1) == 1 : null;
    public int? PixelXDimension => Exif.Get(BmageExifTag.PixelXDimension)?.GetInt32();
    public int? PixelYDimension => Exif.Get(BmageExifTag.PixelYDimension)?.GetInt32();

    /// <summary>The date the photo was taken (DateTimeOriginal, falling back to DateTime). Unspecified kind unless an offset tag exists.</summary>
    public DateTimeOffset? DateTaken => ParseDate(Exif.Get(BmageExifTag.DateTimeOriginal)?.GetString(), Exif.Get(BmageExifTag.OffsetTimeOriginal)?.GetString())
        ?? ParseDate(Primary.Get(BmageExifTag.DateTime)?.GetString(), Exif.Get(BmageExifTag.OffsetTime)?.GetString());

    /// <summary>Parses an EXIF date string ("YYYY:MM:DD HH:MM:SS") with an optional "+HH:MM" offset.</summary>
    public static DateTimeOffset? ParseDate(string? value, string? offset = null)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 19) return null;
        if (!int.TryParse(value.AsSpan(0, 4), out var y) || !int.TryParse(value.AsSpan(5, 2), out var mo) || !int.TryParse(value.AsSpan(8, 2), out var d)
            || !int.TryParse(value.AsSpan(11, 2), out var h) || !int.TryParse(value.AsSpan(14, 2), out var mi) || !int.TryParse(value.AsSpan(17, 2), out var s))
            return null;
        if (y < 1 || mo is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(Math.Min(y, 9999), mo) || h > 23 || mi > 59 || s > 60) return null;
        var offsetSpan = TimeSpan.Zero;
        if (!string.IsNullOrWhiteSpace(offset) && offset.Length >= 6 && int.TryParse(offset.AsSpan(1, 2), out var oh) && int.TryParse(offset.AsSpan(4, 2), out var om))
            offsetSpan = new TimeSpan(offset[0] == '-' ? -oh : oh, offset[0] == '-' ? -om : om, 0);
        try { return new DateTimeOffset(y, mo, d, h, mi, Math.Min(s, 59), offsetSpan); }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    /// <summary>GPS coordinates, when present and well formed.</summary>
    public BmageGpsCoordinates? Location
    {
        get
        {
            var lat = ReadCoordinate(Gps.Get(BmageExifTag.GpsLatitude), Gps.Get(BmageExifTag.GpsLatitudeRef)?.GetString(), "S");
            var lon = ReadCoordinate(Gps.Get(BmageExifTag.GpsLongitude), Gps.Get(BmageExifTag.GpsLongitudeRef)?.GetString(), "W");
            if (lat is null || lon is null) return null;
            double? alt = Gps.Get(BmageExifTag.GpsAltitude)?.GetDouble();
            if (alt is not null && Gps.Get(BmageExifTag.GpsAltitudeRef)?.GetInt32() == 1) alt = -alt;
            return new BmageGpsCoordinates(lat.Value, lon.Value, alt);
        }
    }

    private static double? ReadCoordinate(BmageExifValue? value, string? reference, string negativeRef)
    {
        if (value is null || value.Count < 3) return null;
        var d = value.GetDouble(0); var m = value.GetDouble(1); var s = value.GetDouble(2);
        if (d is null || m is null || s is null) return null;
        var result = d.Value + m.Value / 60 + s.Value / 3600;
        if (string.Equals(reference, negativeRef, StringComparison.OrdinalIgnoreCase)) result = -result;
        return result;
    }

    /// <summary>True when the data contains any GPS information.</summary>
    public bool HasLocation => Gps.Count > 0;

    // ---- Builders ----

    public BmageExifData WithPrimary(BmageExifDirectory primary) => new(primary, Exif, Gps, Interop, Thumbnail);
    public BmageExifData WithExif(BmageExifDirectory exif) => new(Primary, exif, Gps, Interop, Thumbnail);
    public BmageExifData WithGps(BmageExifDirectory gps) => new(Primary, Exif, gps, Interop, Thumbnail);

    public BmageExifData WithOrientation(BmageOrientation orientation)
    {
        if (Orientation == orientation && (orientation != BmageOrientation.Normal || !Primary.Contains(BmageExifTag.Orientation))) return this;
        return WithPrimary(Primary.With(BmageExifTag.Orientation, BmageExifValue.FromShort((ushort)(orientation == BmageOrientation.Unspecified ? 1 : (int)orientation))));
    }

    public BmageExifData WithPixelDimensions(int width, int height)
    {
        if (Exif.Count == 0 && !Exif.Contains(BmageExifTag.PixelXDimension)) return this;
        var exif = Exif.With(BmageExifTag.PixelXDimension, BmageExifValue.FromLong((uint)width)).With(BmageExifTag.PixelYDimension, BmageExifValue.FromLong((uint)height));
        return new BmageExifData(Primary.Without(BmageExifTag.ImageWidth, BmageExifTag.ImageLength), exif, Gps, Interop, Thumbnail);
    }

    public BmageExifData WithoutThumbnail() => Thumbnail.IsEmpty ? this : new BmageExifData(Primary, Exif, Gps, Interop, default);

    public BmageExifData WithoutLocation() => Gps.Count == 0 ? this : new BmageExifData(Primary, Exif, new BmageExifDirectory(), Interop, Thumbnail);

    /// <summary>
    /// The tags <see cref="StripSensitive"/> removes. They go from every directory, not only the one the standard puts
    /// them in: the reader keeps a tag in whichever directory a file put it, and the writer puts it back there.
    /// </summary>
    private static readonly ushort[] SensitiveTags =
    [
        BmageExifTag.GpsIfdPointer, BmageExifTag.Artist, BmageExifTag.WindowsAuthor, BmageExifTag.WindowsComment, BmageExifTag.MakerNote, BmageExifTag.UserComment,
        BmageExifTag.ImageUniqueId, BmageExifTag.OwnerName, BmageExifTag.BodySerialNumber, BmageExifTag.LensSerialNumber,
    ];

    /// <summary>
    /// Removes privacy-sensitive fields: GPS, serial numbers, the owner's and the artist's names (Windows' author
    /// included), user comments (Windows' comment included), maker note, unique id and thumbnail.
    /// </summary>
    public BmageExifData StripSensitive()
        => new(Primary.Without(SensitiveTags), Exif.Without(SensitiveTags), new BmageExifDirectory(), Interop.Without(SensitiveTags), default);

    /// <summary>Creates a minimal EXIF block with the given orientation.</summary>
    public static BmageExifData FromOrientation(BmageOrientation orientation) => new BmageExifData().WithOrientation(orientation);

    /// <summary>Parses an EXIF payload (TIFF structure, optionally prefixed with "Exif\0\0"). Returns null when the data is not EXIF.</summary>
    public static BmageExifData? Parse(ReadOnlySpan<byte> data) => BmageExifReader.Read(data);

    /// <summary>Serialises to a TIFF structure (without the "Exif\0\0" prefix).</summary>
    public byte[] ToTiff() => BmageExifWriter.Write(this);
}
