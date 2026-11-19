namespace Bit.Bmage;

/// <summary>Well known EXIF tag identifiers.</summary>
public static class BmageExifTag
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
    // Windows Explorer writes these from its Details tab, as UTF-16 bytes.
    public const ushort WindowsComment = 0x9C9C;
    public const ushort WindowsAuthor = 0x9C9D;
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
