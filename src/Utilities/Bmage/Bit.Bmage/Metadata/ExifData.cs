using System.Buffers.Binary;
using System.Text;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Metadata;

/// <summary>TIFF/EXIF field data types.</summary>
public enum ExifType : ushort
{
    Byte = 1,
    Ascii = 2,
    Short = 3,
    Long = 4,
    Rational = 5,
    SByte = 6,
    Undefined = 7,
    SShort = 8,
    SLong = 9,
    SRational = 10,
    Float = 11,
    Double = 12,
}

/// <summary>Well known EXIF tag identifiers.</summary>
public static class ExifTag
{
    // IFD0 / TIFF
    public const ushort ImageWidth = 0x0100;
    public const ushort ImageLength = 0x0101;
    public const ushort ImageDescription = 0x010E;
    public const ushort Make = 0x010F;
    public const ushort Model = 0x0110;
    public const ushort Orientation = 0x0112;
    public const ushort XResolution = 0x011A;
    public const ushort YResolution = 0x011B;
    public const ushort ResolutionUnit = 0x0128;
    public const ushort Software = 0x0131;
    public const ushort DateTime = 0x0132;
    public const ushort Artist = 0x013B;
    public const ushort Copyright = 0x8298;
    public const ushort ExifIfdPointer = 0x8769;
    public const ushort GpsIfdPointer = 0x8825;
    public const ushort InteropIfdPointer = 0xA005;

    // EXIF IFD
    public const ushort ExposureTime = 0x829A;
    public const ushort FNumber = 0x829D;
    public const ushort ExposureProgram = 0x8822;
    public const ushort Iso = 0x8827;
    public const ushort ExifVersion = 0x9000;
    public const ushort DateTimeOriginal = 0x9003;
    public const ushort DateTimeDigitized = 0x9004;
    public const ushort OffsetTime = 0x9010;
    public const ushort OffsetTimeOriginal = 0x9011;
    public const ushort ExposureBias = 0x9204;
    public const ushort MeteringMode = 0x9207;
    public const ushort Flash = 0x9209;
    public const ushort FocalLength = 0x920A;
    public const ushort MakerNote = 0x927C;
    public const ushort UserComment = 0x9286;
    public const ushort SubSecTime = 0x9290;
    public const ushort SubSecTimeOriginal = 0x9291;
    public const ushort ColorSpace = 0xA001;
    public const ushort PixelXDimension = 0xA002;
    public const ushort PixelYDimension = 0xA003;
    public const ushort WhiteBalance = 0xA403;
    public const ushort FocalLengthIn35mmFilm = 0xA405;
    public const ushort ImageUniqueId = 0xA420;
    public const ushort OwnerName = 0xA430;
    public const ushort BodySerialNumber = 0xA431;
    public const ushort LensSpecification = 0xA432;
    public const ushort LensMake = 0xA433;
    public const ushort LensModel = 0xA434;
    public const ushort LensSerialNumber = 0xA435;

    // GPS IFD
    public const ushort GpsVersionId = 0x0000;
    public const ushort GpsLatitudeRef = 0x0001;
    public const ushort GpsLatitude = 0x0002;
    public const ushort GpsLongitudeRef = 0x0003;
    public const ushort GpsLongitude = 0x0004;
    public const ushort GpsAltitudeRef = 0x0005;
    public const ushort GpsAltitude = 0x0006;
    public const ushort GpsTimeStamp = 0x0007;
    public const ushort GpsDateStamp = 0x001D;
}

/// <summary>A single EXIF field with its raw little-endian value bytes.</summary>
public sealed class ExifValue
{
    public ExifValue(ExifType type, int count, ReadOnlyMemory<byte> data)
    {
        Type = type;
        Count = count;
        Data = data;
    }

    public ExifType Type { get; }

    /// <summary>Number of components (characters for ASCII).</summary>
    public int Count { get; }

    /// <summary>Raw value bytes normalised to little-endian byte order.</summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>Size in bytes of one component of the given type.</summary>
    public static int SizeOf(ExifType type) => type switch
    {
        ExifType.Byte or ExifType.Ascii or ExifType.SByte or ExifType.Undefined => 1,
        ExifType.Short or ExifType.SShort => 2,
        ExifType.Long or ExifType.SLong or ExifType.Float => 4,
        ExifType.Rational or ExifType.SRational or ExifType.Double => 8,
        _ => 1,
    };

    /// <summary>
    /// Creates a text field. EXIF names this type ASCII, but cameras and editors have written UTF-8 into these fields
    /// for years, so UTF-8 is what this library writes and reads. Restricting to ASCII would silently mangle a
    /// copyright symbol or a name with an accent, which is exactly the data a preserve policy exists to protect.
    /// </summary>
    public static ExifValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value + "\0");
        return new ExifValue(ExifType.Ascii, bytes.Length, bytes);
    }

    public static ExifValue FromShort(params ushort[] values)
    {
        var bytes = new byte[values.Length * 2];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), values[i]);
        return new ExifValue(ExifType.Short, values.Length, bytes);
    }

    public static ExifValue FromLong(params uint[] values)
    {
        var bytes = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), values[i]);
        return new ExifValue(ExifType.Long, values.Length, bytes);
    }

    public static ExifValue FromRational(params (uint Numerator, uint Denominator)[] values)
    {
        var bytes = new byte[values.Length * 8];
        for (var i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 8), values[i].Numerator);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 8 + 4), values[i].Denominator);
        }
        return new ExifValue(ExifType.Rational, values.Length, bytes);
    }

    public static ExifValue FromUndefined(ReadOnlyMemory<byte> bytes) => new(ExifType.Undefined, bytes.Length, bytes);

    /// <summary>Reads the value as a string (ASCII fields; trailing NULs trimmed).</summary>
    public string? GetString()
    {
        if (Type is not (ExifType.Ascii or ExifType.Undefined or ExifType.Byte)) return null;
        var span = Data.Span;
        var end = span.Length;
        while (end > 0 && span[end - 1] == 0) end--;
        return Encoding.UTF8.GetString(span[..end]).Trim();
    }

    /// <summary>Reads the component at <paramref name="index"/> as a double (integers, rationals and floats).</summary>
    public double? GetDouble(int index = 0)
    {
        if (index < 0 || index >= Count) return null;
        var s = Data.Span;
        var sz = SizeOf(Type);
        if ((index + 1) * sz > s.Length) return null;
        var v = s.Slice(index * sz, sz);
        return Type switch
        {
            ExifType.Byte or ExifType.Undefined => v[0],
            ExifType.SByte => (sbyte)v[0],
            ExifType.Short => BinaryPrimitives.ReadUInt16LittleEndian(v),
            ExifType.SShort => BinaryPrimitives.ReadInt16LittleEndian(v),
            ExifType.Long => BinaryPrimitives.ReadUInt32LittleEndian(v),
            ExifType.SLong => BinaryPrimitives.ReadInt32LittleEndian(v),
            ExifType.Rational => Ratio(BinaryPrimitives.ReadUInt32LittleEndian(v), BinaryPrimitives.ReadUInt32LittleEndian(v[4..])),
            ExifType.SRational => Ratio(BinaryPrimitives.ReadInt32LittleEndian(v), BinaryPrimitives.ReadInt32LittleEndian(v[4..])),
            ExifType.Float => BinaryPrimitives.ReadSingleLittleEndian(v),
            ExifType.Double => BinaryPrimitives.ReadDoubleLittleEndian(v),
            _ => null,
        };

        static double? Ratio(double n, double d) => d == 0 ? (n == 0 ? 0 : null) : n / d;
    }

    /// <summary>Reads the first component as an integer.</summary>
    public int? GetInt32(int index = 0) => GetDouble(index) is { } d && !double.IsNaN(d) ? (int)d : null;

    public override string ToString() => Type == ExifType.Ascii ? GetString() ?? "" : Count == 1 ? GetDouble()?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "" : $"{Type}[{Count}]";
}

/// <summary>A single image file directory: an ordered map of tag → value.</summary>
public sealed class ExifDirectory
{
    private readonly SortedDictionary<ushort, ExifValue> _tags;

    public ExifDirectory() : this(new SortedDictionary<ushort, ExifValue>()) { }

    public ExifDirectory(IDictionary<ushort, ExifValue> tags) => _tags = new SortedDictionary<ushort, ExifValue>(tags);

    public IReadOnlyDictionary<ushort, ExifValue> Tags => _tags;
    public int Count => _tags.Count;

    public ExifValue? Get(ushort tag) => _tags.TryGetValue(tag, out var v) ? v : null;
    public bool Contains(ushort tag) => _tags.ContainsKey(tag);

    /// <summary>Returns a copy with the tag set (or removed when <paramref name="value"/> is null).</summary>
    public ExifDirectory With(ushort tag, ExifValue? value)
    {
        var copy = new ExifDirectory(_tags);
        if (value is null) copy._tags.Remove(tag); else copy._tags[tag] = value;
        return copy;
    }

    /// <summary>Returns a copy without the listed tags.</summary>
    public ExifDirectory Without(params ushort[] tags)
    {
        var copy = new ExifDirectory(_tags);
        foreach (var t in tags) copy._tags.Remove(t);
        return copy;
    }
}

/// <summary>GPS coordinates decoded from EXIF.</summary>
public readonly record struct GpsCoordinates(double Latitude, double Longitude, double? Altitude)
{
    public override string ToString() => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Latitude:0.######}, {Longitude:0.######}{(Altitude is { } a ? $", {a:0.#}m" : "")}");
}

/// <summary>
/// Parsed EXIF metadata: the primary (IFD0), EXIF, GPS and interoperability directories plus an optional embedded thumbnail.
/// Typed accessors are provided for common fields; every tag remains available through the directories.
/// </summary>
public sealed class ExifData
{
    public ExifData(ExifDirectory? primary = null, ExifDirectory? exif = null, ExifDirectory? gps = null, ExifDirectory? interop = null, ReadOnlyMemory<byte> thumbnail = default)
    {
        Primary = primary ?? new ExifDirectory();
        Exif = exif ?? new ExifDirectory();
        Gps = gps ?? new ExifDirectory();
        Interop = interop ?? new ExifDirectory();
        Thumbnail = thumbnail;
    }

    /// <summary>IFD0: camera make/model, orientation, software, dates, copyright.</summary>
    public ExifDirectory Primary { get; }
    /// <summary>The EXIF sub-IFD: exposure settings, original date, lens, pixel dimensions.</summary>
    public ExifDirectory Exif { get; }
    /// <summary>The GPS sub-IFD.</summary>
    public ExifDirectory Gps { get; }
    /// <summary>The interoperability sub-IFD.</summary>
    public ExifDirectory Interop { get; }
    /// <summary>Embedded JPEG thumbnail bytes from IFD1, or empty.</summary>
    public ReadOnlyMemory<byte> Thumbnail { get; }

    public bool IsEmpty => Primary.Count == 0 && Exif.Count == 0 && Gps.Count == 0 && Interop.Count == 0;

    // ---- Typed accessors ----

    public Orientation Orientation
    {
        get
        {
            var v = Primary.Get(ExifTag.Orientation)?.GetInt32();
            return v is >= 1 and <= 8 ? (Orientation)v : Orientation.Normal;
        }
    }

    public string? Make => Primary.Get(ExifTag.Make)?.GetString();
    public string? Model => Primary.Get(ExifTag.Model)?.GetString();
    public string? Software => Primary.Get(ExifTag.Software)?.GetString();
    public string? Artist => Primary.Get(ExifTag.Artist)?.GetString();
    public string? Copyright => Primary.Get(ExifTag.Copyright)?.GetString();
    public string? ImageDescription => Primary.Get(ExifTag.ImageDescription)?.GetString();
    public string? LensModel => Exif.Get(ExifTag.LensModel)?.GetString();
    public string? UserComment => Exif.Get(ExifTag.UserComment)?.GetString();

    /// <summary>Exposure time in seconds.</summary>
    public double? ExposureTime => Exif.Get(ExifTag.ExposureTime)?.GetDouble();
    public double? FNumber => Exif.Get(ExifTag.FNumber)?.GetDouble();
    public int? Iso => Exif.Get(ExifTag.Iso)?.GetInt32();
    /// <summary>Focal length in millimetres.</summary>
    public double? FocalLength => Exif.Get(ExifTag.FocalLength)?.GetDouble();
    public int? FocalLengthIn35mmFilm => Exif.Get(ExifTag.FocalLengthIn35mmFilm)?.GetInt32();
    public double? ExposureBias => Exif.Get(ExifTag.ExposureBias)?.GetDouble();
    public bool? FlashFired => Exif.Get(ExifTag.Flash)?.GetInt32() is { } f ? (f & 1) == 1 : null;
    public int? PixelXDimension => Exif.Get(ExifTag.PixelXDimension)?.GetInt32();
    public int? PixelYDimension => Exif.Get(ExifTag.PixelYDimension)?.GetInt32();

    /// <summary>The date the photo was taken (DateTimeOriginal, falling back to DateTime). Unspecified kind unless an offset tag exists.</summary>
    public DateTimeOffset? DateTaken => ParseDate(Exif.Get(ExifTag.DateTimeOriginal)?.GetString(), Exif.Get(ExifTag.OffsetTimeOriginal)?.GetString())
        ?? ParseDate(Primary.Get(ExifTag.DateTime)?.GetString(), Exif.Get(ExifTag.OffsetTime)?.GetString());

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
    public GpsCoordinates? Location
    {
        get
        {
            var lat = ReadCoordinate(Gps.Get(ExifTag.GpsLatitude), Gps.Get(ExifTag.GpsLatitudeRef)?.GetString(), "S");
            var lon = ReadCoordinate(Gps.Get(ExifTag.GpsLongitude), Gps.Get(ExifTag.GpsLongitudeRef)?.GetString(), "W");
            if (lat is null || lon is null) return null;
            double? alt = Gps.Get(ExifTag.GpsAltitude)?.GetDouble();
            if (alt is not null && Gps.Get(ExifTag.GpsAltitudeRef)?.GetInt32() == 1) alt = -alt;
            return new GpsCoordinates(lat.Value, lon.Value, alt);
        }
    }

    private static double? ReadCoordinate(ExifValue? value, string? reference, string negativeRef)
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

    public ExifData WithPrimary(ExifDirectory primary) => new(primary, Exif, Gps, Interop, Thumbnail);
    public ExifData WithExif(ExifDirectory exif) => new(Primary, exif, Gps, Interop, Thumbnail);
    public ExifData WithGps(ExifDirectory gps) => new(Primary, Exif, gps, Interop, Thumbnail);

    public ExifData WithOrientation(Orientation orientation)
    {
        if (Orientation == orientation && (orientation != Orientation.Normal || !Primary.Contains(ExifTag.Orientation))) return this;
        return WithPrimary(Primary.With(ExifTag.Orientation, ExifValue.FromShort((ushort)(orientation == Orientation.Unspecified ? 1 : (int)orientation))));
    }

    public ExifData WithPixelDimensions(int width, int height)
    {
        if (Exif.Count == 0 && !Exif.Contains(ExifTag.PixelXDimension)) return this;
        var exif = Exif.With(ExifTag.PixelXDimension, ExifValue.FromLong((uint)width)).With(ExifTag.PixelYDimension, ExifValue.FromLong((uint)height));
        return new ExifData(Primary.Without(ExifTag.ImageWidth, ExifTag.ImageLength), exif, Gps, Interop, Thumbnail);
    }

    public ExifData WithoutThumbnail() => Thumbnail.IsEmpty ? this : new ExifData(Primary, Exif, Gps, Interop, default);

    public ExifData WithoutLocation() => Gps.Count == 0 ? this : new ExifData(Primary, Exif, new ExifDirectory(), Interop, Thumbnail);

    /// <summary>Removes privacy-sensitive fields: GPS, serial numbers, owner, user comment, maker note, unique id and thumbnail.</summary>
    public ExifData StripSensitive()
    {
        var exif = Exif.Without(ExifTag.MakerNote, ExifTag.UserComment, ExifTag.ImageUniqueId, ExifTag.OwnerName, ExifTag.BodySerialNumber, ExifTag.LensSerialNumber);
        return new ExifData(Primary.Without(ExifTag.GpsIfdPointer), exif, new ExifDirectory(), Interop, default);
    }

    /// <summary>Creates a minimal EXIF block with the given orientation.</summary>
    public static ExifData FromOrientation(Orientation orientation) => new ExifData().WithOrientation(orientation);

    /// <summary>Parses an EXIF payload (TIFF structure, optionally prefixed with "Exif\0\0"). Returns null when the data is not EXIF.</summary>
    public static ExifData? Parse(ReadOnlySpan<byte> data) => ExifReader.Read(data);

    /// <summary>Serialises to a TIFF structure (without the "Exif\0\0" prefix).</summary>
    public byte[] ToTiff() => ExifWriter.Write(this);
}
