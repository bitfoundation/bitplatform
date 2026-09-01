namespace Bit.BlazorUI;

/// <summary>
/// Determines how the image is scaled and cropped to fit the frame of a <see cref="BitImage"/>.
/// </summary>
/// <remarks>
/// The frame is the element around the image, whose size comes from the Width, the Height, the
/// AspectRatio and the MaximizeFrame parameters. This enum decides what happens inside it once the
/// image and the frame turn out to be different shapes: the image is left alone, stretched to the
/// frame, fitted inside it, or made to fill it with the overflow cropped away.
/// </remarks>
public enum BitImageFit
{
    /// <summary>
    /// Neither the image nor the frame are scaled. The image keeps its natural size and whatever of
    /// it does not fit the frame is cropped away from the right and the bottom.
    /// </summary>
    None,

    /// <summary>
    /// The image is not scaled. The image is centered and cropped within the content box.
    /// </summary>
    Center,

    /// <summary>
    /// The image will be centered horizontally and vertically within the frame and maintains its
    /// aspect ratio, scaled down where needed so that all of it fits inside the frame.
    /// </summary>
    CenterContain,

    /// <summary>
    /// The image will be centered horizontally and vertically within the frame and maintains its
    /// aspect ratio, scaled up where needed so that it covers the frame and the overflow is cropped.
    /// </summary>
    CenterCover,

    /// <summary>
    /// The image is scaled to maintain its aspect ratio while being fully contained within the frame.
    /// Nothing is cropped, and whatever of the frame the image does not reach is left empty.
    /// </summary>
    Contain,

    /// <summary>
    /// The image is scaled to maintain its aspect ratio while filling the frame. Nothing of the frame
    /// is left empty, and whatever of the image falls outside it is cropped away.
    /// </summary>
    Cover,

    /// <summary>
    /// The image is stretched to fill the frame exactly, without maintaining its aspect ratio. Nothing
    /// is cropped and nothing is left empty, at the cost of distorting the image where the two shapes
    /// disagree.
    /// </summary>
    Fill,

    /// <summary>
    /// The image is contained within the frame, but never scaled up: an image smaller than the frame
    /// keeps its natural size. This is <see cref="Contain"/> for a frame that must not blur a small
    /// image by enlarging it.
    /// </summary>
    ScaleDown
}
