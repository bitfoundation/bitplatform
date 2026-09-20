namespace Bit.BlazorUI;

/// <summary>
/// The orientation a <see cref="BitImageFit.CenterCover"/> or <see cref="BitImageFit.CenterContain"/>
/// image is fitted along in a <see cref="BitImage"/>.
/// </summary>
/// <remarks>
/// Those two fits center the image in the frame and scale it along one axis, and this is the axis they
/// scale along. It is the shape of the FRAME rather than of the image: a wide frame is
/// <see cref="Landscape"/> and a tall one is <see cref="Portrait"/>, which is also the default.
/// <br />
/// It has no effect on any other <see cref="BitImageFit"/>, since those are expressed with the
/// object-fit property, which reads the two shapes on its own.
/// </remarks>
public enum BitImageCover
{
    /// <summary>
    /// The image will be shown at 100% height of container and the width will be scaled accordingly.
    /// </summary>
    Landscape,

    /// <summary>
    /// The image will be shown at 100% width of container and the height will be scaled accordingly.
    /// </summary>
    Portrait,
}
