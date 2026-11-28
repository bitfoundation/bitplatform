namespace Bit.BlazorUI;

/// <summary>
/// Represents the values of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/loading">loading attribute</see>
/// of an img or an iframe element.
/// </summary>
public enum BitImageLoading
{
    /// <summary>
    /// Tells the browser to load the image or the embedded page as soon as the element is processed, which is what a
    /// browser does with an img or an iframe that has no loading attribute.
    /// </summary>
    Eager,

    /// <summary>
    /// Tells the user agent to hold off on loading the image or the embedded page until the browser estimates that it
    /// will be needed imminently.
    /// </summary>
    Lazy
}
