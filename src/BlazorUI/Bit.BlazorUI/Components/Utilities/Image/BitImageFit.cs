namespace Bit.BlazorUI;

public enum BitImageFit
{
    /// <summary>
    /// Neither the image nor the frame are scaled.
    /// </summary>
    None,

    /// <summary>
    /// The image is not scaled. The image is centered and cropped within the content box.
    /// </summary>
    Center,

    /// <summary>
    /// The image will be centered horizontally and vertically within the frame and maintains its aspect ratio.
    /// </summary>
    CenterContain,

    /// <summary>
    /// The image will be centered horizontally and vertically within the frame and maintains its aspect ratio.
    /// </summary>
    CenterCover,

    /// <summary>
    /// The image is scaled to maintain its aspect ratio while being fully contained within the frame.
    /// </summary>
    Contain,

    /// <summary>
    /// The image is scaled to maintain its aspect ratio while filling the frame.
    /// </summary>
    Cover,
}
