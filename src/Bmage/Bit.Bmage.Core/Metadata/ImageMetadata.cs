using Bit.Bmage.Geometry;

namespace Bit.Bmage.Metadata;

/// <summary>Controls which metadata is carried into exported images.</summary>
public enum MetadataPolicy
{
    /// <summary>
    /// Remove all metadata (EXIF, XMP, ICC profile). The safest default for user uploads: no location, device or
    /// timestamp information leaks and files are smallest.
    /// </summary>
    /// <remarks>
    /// This removes the orientation tag along with everything else, so the exported pixels must already be upright.
    /// They are whenever the image was decoded with <see cref="Codecs.DecodeOptions.AutoOrient"/> left on, which is
    /// the default. If you turn it off, rotate the pixels yourself (an <c>AutoOrient</c> pipeline step does it) before
    /// exporting with this policy, or the image will be saved sideways with no tag left to correct it.
    /// </remarks>
    Remove = 0,

    /// <summary>
    /// Keep descriptive metadata (camera, exposure, dates, copyright, ICC profile) but strip privacy-sensitive fields:
    /// GPS location, serial numbers, owner name, user comments, maker notes, unique IDs and embedded thumbnails.
    /// </summary>
    StripSensitive,

    /// <summary>Keep all metadata that the target format can carry. Orientation and pixel dimensions are still corrected.</summary>
    Preserve,
}

/// <summary>
/// Immutable metadata attached to an <see cref="ImageBuffer"/>: EXIF, ICC colour profile, XMP packet and the source format.
/// Operations pass metadata through unchanged; encoders apply a <see cref="MetadataPolicy"/> when writing.
/// </summary>
public sealed class ImageMetadata
{
    public static ImageMetadata Empty { get; } = new();

    public ImageMetadata(ExifData? exif = null, ReadOnlyMemory<byte> iccProfile = default, ReadOnlyMemory<byte> xmp = default, ImageFormat sourceFormat = ImageFormat.Unknown, string? fileName = null)
    {
        Exif = exif;
        IccProfile = iccProfile;
        Xmp = xmp;
        SourceFormat = sourceFormat;
        FileName = fileName;
    }

    /// <summary>Parsed EXIF data, or null when the source had none.</summary>
    public ExifData? Exif { get; }

    /// <summary>Raw ICC colour profile bytes, or empty.</summary>
    public ReadOnlyMemory<byte> IccProfile { get; }

    /// <summary>Raw XMP packet (UTF-8 XML), or empty.</summary>
    public ReadOnlyMemory<byte> Xmp { get; }

    /// <summary>The format the pixels were decoded from.</summary>
    public ImageFormat SourceFormat { get; }

    /// <summary>The original file name, when known.</summary>
    public string? FileName { get; }

    /// <summary>True when no metadata of any kind is present.</summary>
    public bool IsEmpty => Exif is null && IccProfile.IsEmpty && Xmp.IsEmpty;

    /// <summary>The EXIF orientation, or <see cref="Orientation.Normal"/> when not present.</summary>
    public Orientation Orientation => Exif?.Orientation ?? Orientation.Normal;

    public ImageMetadata WithExif(ExifData? exif) => new(exif, IccProfile, Xmp, SourceFormat, FileName);
    public ImageMetadata WithOrientation(Orientation orientation) => Exif is null ? this : WithExif(Exif.WithOrientation(orientation));
    public ImageMetadata WithIccProfile(ReadOnlyMemory<byte> icc) => new(Exif, icc, Xmp, SourceFormat, FileName);
    public ImageMetadata WithXmp(ReadOnlyMemory<byte> xmp) => new(Exif, IccProfile, xmp, SourceFormat, FileName);
    public ImageMetadata WithSourceFormat(ImageFormat format) => new(Exif, IccProfile, Xmp, format, FileName);
    public ImageMetadata WithFileName(string? fileName) => new(Exif, IccProfile, Xmp, SourceFormat, fileName);

    /// <summary>
    /// Applies a policy and returns the metadata that should be written to an output image of the given size.
    /// EXIF pixel dimensions are updated to match the exported pixels.
    /// </summary>
    /// <remarks>
    /// The orientation tag is carried through rather than reset, because <see cref="Orientation"/> describes the
    /// pixels currently in the buffer. Every decode path maintains that invariant: decoding with
    /// <see cref="Codecs.DecodeOptions.AutoOrient"/> rotates the pixels upright and sets the tag to
    /// <see cref="Geometry.Orientation.Normal"/>, so a normal export writes no rotation. Decoding without it leaves
    /// the pixels in sensor order and the tag describing them, and forcing the tag to normal there would export a
    /// sideways image with nothing left to say so.
    /// </remarks>
    public ImageMetadata Apply(MetadataPolicy policy, int width, int height)
    {
        switch (policy)
        {
            case MetadataPolicy.Remove:
                return new ImageMetadata(null, default, default, SourceFormat, FileName);
            case MetadataPolicy.StripSensitive:
            {
                var exif = Exif?.StripSensitive().WithPixelDimensions(width, height);
                // XMP can carry GPS and device identifiers too; it is dropped by this policy.
                return new ImageMetadata(exif, IccProfile, default, SourceFormat, FileName);
            }
            default:
            {
                var exif = Exif?.WithPixelDimensions(width, height).WithoutThumbnail();
                return new ImageMetadata(exif, IccProfile, Xmp, SourceFormat, FileName);
            }
        }
    }
}
