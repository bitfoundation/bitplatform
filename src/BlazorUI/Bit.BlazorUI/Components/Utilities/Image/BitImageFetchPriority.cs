namespace Bit.BlazorUI;

/// <summary>
/// Represents the img fetchpriority attribute values explained here:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/fetchPriority"/>
/// </summary>
/// <remarks>
/// The attribute reorders this image against everything else the page is fetching at the same time.
/// It is worth setting on exactly two kinds of image: the one that is the largest thing on the first
/// screen - the Largest Contentful Paint element, which is what the page is measured by - and the
/// decorative ones far below it that should never compete with it.
/// </remarks>
public enum BitImageFetchPriority
{
    /// <summary>
    /// The default behavior, which leaves the priority to the browser's own heuristics.
    /// </summary>
    Auto,

    /// <summary>
    /// Fetches the image ahead of the other images of the page. This is for the one image that is the
    /// page's largest contentful paint - a hero, the first photo of an article - and marking several
    /// images high only takes the advantage away from all of them.
    /// </summary>
    High,

    /// <summary>
    /// Fetches the image after the other images of the page, for the ones that carry no meaning on
    /// the first screen - a decorative background, an avatar in a footer, a thumbnail far below.
    /// </summary>
    Low
}
