namespace Bit.Bmage;

/// <summary>Controls which metadata is carried into exported images.</summary>
public enum BmageMetadataPolicy
{
    /// <summary>
    /// Remove all metadata (EXIF, XMP, ICC profile). The safest default for user uploads: no location, device or
    /// timestamp information leaks and files are smallest.
    /// </summary>
    /// <remarks>
    /// This removes the orientation tag along with everything else, so the exported pixels must already be upright.
    /// They are whenever the image was decoded with <see cref="BmageDecodeOptions.AutoOrient"/> left on, which is
    /// the default. If you turn it off, rotate the pixels yourself (an <c>AutoOrient</c> pipeline step does it) before
    /// exporting with this policy, or the image will be saved sideways with no tag left to correct it.
    /// </remarks>
    Remove = 0,

    /// <summary>
    /// Keep descriptive metadata (camera, exposure, dates, copyright, ICC profile) but strip privacy-sensitive fields:
    /// GPS location, serial numbers, owner and artist names, user comments, maker notes, unique IDs and embedded thumbnails.
    /// </summary>
    StripSensitive,

    /// <summary>Keep all metadata that the target format can carry. Orientation and pixel dimensions are still corrected.</summary>
    Preserve,
}
