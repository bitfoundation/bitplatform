namespace Bit.BlazorUI;

/// <summary>
/// Represents the img decoding attribute values explained here:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/decoding"/>
/// </summary>
/// <remarks>
/// The attribute is a hint about when the browser is allowed to spend the time it takes to turn the
/// downloaded bytes into pixels, which for a large image is long enough to be seen as a dropped frame.
/// It is only a hint: the browser decides, and the default is already the right answer nearly always.
/// </remarks>
public enum BitImageDecoding
{
    /// <summary>
    /// The default behavior, which leaves the decision to the browser.
    /// </summary>
    Auto,

    /// <summary>
    /// Decodes the image synchronously, so it is presented together with the rest of the content
    /// rendered in the same frame. This is what keeps an image from appearing a frame after the
    /// text around it, at the cost of holding that frame back until the decoding is done.
    /// </summary>
    Sync,

    /// <summary>
    /// Decodes the image asynchronously, so the rest of the content is not held back while the
    /// decoding runs. This is what keeps a large image from stalling the page it arrives on.
    /// </summary>
    Async
}
