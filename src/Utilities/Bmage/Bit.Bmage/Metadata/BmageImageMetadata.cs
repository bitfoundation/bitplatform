namespace Bit.Bmage;

/// <summary>
/// Immutable metadata attached to an <see cref="BmageImageBuffer"/>: EXIF, ICC colour profile, XMP packet and the source format.
/// Operations pass metadata through unchanged; encoders apply a <see cref="BmageMetadataPolicy"/> when writing.
/// </summary>
public sealed class BmageImageMetadata
{
    public static BmageImageMetadata Empty { get; } = new();

    public BmageImageMetadata(BmageExifData? exif = null, ReadOnlyMemory<byte> iccProfile = default, ReadOnlyMemory<byte> xmp = default, BmageImageFormat sourceFormat = BmageImageFormat.Unknown, string? fileName = null)
    {
        Exif = exif;
        IccProfile = iccProfile;
        Xmp = xmp;
        SourceFormat = sourceFormat;
        FileName = fileName;
    }

    /// <summary>Parsed EXIF data, or null when the source had none.</summary>
    public BmageExifData? Exif { get; }

    /// <summary>Raw ICC colour profile bytes, or empty.</summary>
    public ReadOnlyMemory<byte> IccProfile { get; }

    /// <summary>Raw XMP packet (UTF-8 XML), or empty.</summary>
    public ReadOnlyMemory<byte> Xmp { get; }

    /// <summary>The format the pixels were decoded from.</summary>
    public BmageImageFormat SourceFormat { get; }

    /// <summary>The original file name, when known.</summary>
    public string? FileName { get; }

    /// <summary>True when no metadata of any kind is present.</summary>
    public bool IsEmpty => Exif is null && IccProfile.IsEmpty && Xmp.IsEmpty;

    /// <summary>The EXIF orientation, or <see cref="BmageOrientation.Normal"/> when not present.</summary>
    public BmageOrientation Orientation => Exif?.Orientation ?? BmageOrientation.Normal;

    public BmageImageMetadata WithExif(BmageExifData? exif) => new(exif, IccProfile, Xmp, SourceFormat, FileName);
    public BmageImageMetadata WithOrientation(BmageOrientation orientation) => Exif is null ? this : WithExif(Exif.WithOrientation(orientation));
    public BmageImageMetadata WithIccProfile(ReadOnlyMemory<byte> icc) => new(Exif, icc, Xmp, SourceFormat, FileName);
    public BmageImageMetadata WithXmp(ReadOnlyMemory<byte> xmp) => new(Exif, IccProfile, xmp, SourceFormat, FileName);
    public BmageImageMetadata WithSourceFormat(BmageImageFormat format) => new(Exif, IccProfile, Xmp, format, FileName);
    public BmageImageMetadata WithFileName(string? fileName) => new(Exif, IccProfile, Xmp, SourceFormat, fileName);

    /// <summary>
    /// Applies a policy and returns the metadata that should be written to an output image of the given size.
    /// EXIF pixel dimensions are updated to match the exported pixels.
    /// </summary>
    /// <remarks>
    /// The orientation tag is carried through rather than reset, because <see cref="BmageOrientation"/> describes the
    /// pixels currently in the buffer. Every decode path maintains that invariant: decoding with
    /// <see cref="BmageDecodeOptions.AutoOrient"/> rotates the pixels upright and sets the tag to
    /// <see cref="BmageOrientation.Normal"/>, so a normal export writes no rotation. Decoding without it leaves
    /// the pixels in sensor order and the tag describing them, and forcing the tag to normal there would export a
    /// sideways image with nothing left to say so.
    /// </remarks>
    public BmageImageMetadata Apply(BmageMetadataPolicy policy, int width, int height)
    {
        switch (policy)
        {
            case BmageMetadataPolicy.Remove:
                return new BmageImageMetadata(null, default, default, SourceFormat, FileName);
            case BmageMetadataPolicy.StripSensitive:
            {
                var exif = Exif?.StripSensitive().WithPixelDimensions(width, height);
                // XMP can carry GPS and device identifiers too; it is dropped by this policy.
                return new BmageImageMetadata(exif, IccProfile, default, SourceFormat, FileName);
            }
            default:
            {
                var exif = Exif?.WithPixelDimensions(width, height).WithoutThumbnail();
                return new BmageImageMetadata(exif, IccProfile, Xmp, SourceFormat, FileName);
            }
        }
    }
}
