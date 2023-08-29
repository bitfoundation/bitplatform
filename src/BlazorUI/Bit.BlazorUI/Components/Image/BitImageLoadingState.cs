namespace Bit.BlazorUI;

public enum BitImageLoadingState
{
    /// <summary>
    /// The image has not yet been loaded, and there is no error yet.
    /// </summary>
    NotLoaded,

    /// <summary>
    /// The image has been loaded successfully.
    /// </summary>
    Loaded,

    /// <summary>
    /// An error has been encountered while loading the image.
    /// </summary>
    Error,

    /// <summary>
    /// Not used. Use `OnLoadingStateChange` and re-render the Image with a different src.
    /// </summary>
    ErrorLoaded,
}
