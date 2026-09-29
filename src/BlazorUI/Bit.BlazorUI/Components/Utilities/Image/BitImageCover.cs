namespace Bit.BlazorUI;

/// <summary>
/// The shape of the image relative to its frame, which a <see cref="BitImageFit.CenterCover"/> or a
/// <see cref="BitImageFit.CenterContain"/> image of a <see cref="BitImage"/> is fitted by.
/// </summary>
/// <remarks>
/// Those two fits center the image and scale it along one axis, and which axis is the right one depends
/// on how the two shapes compare - which a stylesheet cannot read off the image. So it is said here: an
/// image proportionally WIDER than its frame (a 2:1 logo in a square frame) is <see cref="Landscape"/>,
/// and one proportionally TALLER (the same logo in a 3:1 banner) is <see cref="Portrait"/>, which is also
/// the default. It is the two shapes compared, not the shape of either one: a wide photograph in a wider
/// frame is Portrait.
/// <br />
/// It has no effect on any other <see cref="BitImageFit"/>, since those are expressed with the
/// object-fit property, which compares the two shapes on its own.
/// </remarks>
public enum BitImageCover
{
    /// <summary>
    /// The image is proportionally wider than its frame: <see cref="BitImageFit.CenterCover"/> fits its
    /// height to the frame and crops the sides, <see cref="BitImageFit.CenterContain"/> fits its width.
    /// </summary>
    Landscape,

    /// <summary>
    /// The image is proportionally taller than its frame: <see cref="BitImageFit.CenterCover"/> fits its
    /// width to the frame and crops the top and the bottom, <see cref="BitImageFit.CenterContain"/> fits
    /// its height.
    /// </summary>
    Portrait,
}
