namespace Bit.BlazorUI;

/// <summary>
/// Represents the img loading attribute values explained here:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/loading"/>
/// </summary>
public enum BitImageLoading
{
    /// <summary>
    /// The default behavior, eager tells the browser to load the image as soon as the img element is processed.
    /// </summary>
    Eager,

    /// <summary>
    /// Tells the user agent to hold off on loading the image until the browser estimates that it will be needed imminently.
    /// </summary>
    Lazy
}
