namespace Bit.BlazorUI;

public enum BitImageState
{
    /// <summary>
    /// The image is loading from its source.
    /// </summary>
    Loading,

    /// <summary>
    /// The image has been loaded successfully.
    /// </summary>
    Loaded,

    /// <summary>
    /// An error has been encountered while loading the image.
    /// </summary>
    Error
}
