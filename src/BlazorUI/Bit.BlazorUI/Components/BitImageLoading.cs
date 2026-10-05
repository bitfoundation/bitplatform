namespace Bit.BlazorUI;

/// <summary>
/// Represents the img loading attribute values explained here:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/loading"/>
/// </summary>
public enum BitImageLoading
{
    /// <summary>
    /// Tells the browser to load the image as soon as the img element is processed, which is what a browser does with
    /// an img that has no loading attribute.
    /// </summary>
    Eager,

    /// <summary>
    /// Tells the user agent to hold off on loading the image until the browser estimates that it will be needed imminently.
    /// </summary>
    Lazy
}
