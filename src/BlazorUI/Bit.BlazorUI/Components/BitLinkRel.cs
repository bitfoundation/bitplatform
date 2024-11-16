namespace Bit.BlazorUI;

/// <summary>
/// The rel attribute defines the relationship between a linked resource and the current document.
/// </summary>
[Flags]
public enum BitLinkRel
{
    /// <summary>
    /// Provides a link to an alternate representation of the document. (i.e. print page, translated or mirror)
    /// </summary>
    Alternate = 1,

    /// <summary>
    /// Provides a link to the author of the document.
    /// </summary>
    Author = 2,

    /// <summary>
    /// Permanent URL used for bookmarking.
    /// </summary>
    Bookmark = 4,

    /// <summary>
    /// Indicates that the referenced document is not part of the same site as the current document.
    /// </summary>
    External = 8,

    /// <summary>
    /// Provides a link to a help document.
    /// </summary>
    Help = 16,

    /// <summary>
    /// Provides a link to licensing information for the document.
    /// </summary>
    License = 32,

    /// <summary>
    /// Provides a link to the next document in the series.
    /// </summary>
    Next = 64,

    /// <summary>
    /// Links to an unendorsed document, like a paid link.
    /// ("NoFollow" is used by Google, to specify that the Google search spider should not follow that link)
    /// </summary>
    NoFollow = 128,

    /// <summary>
    /// Requires that any browsing context created by following the hyperlink must not have an opener browsing context.
    /// </summary>
    NoOpener = 256,

    /// <summary>
    /// Makes the referrer unknown. No referrer header will be included when the user clicks the hyperlink.
    /// </summary>
    NoReferrer = 512,

    /// <summary>
    /// The previous document in a selection.
    /// </summary>
    Prev = 1024,

    /// <summary>
    /// Links to a search tool for the document.
    /// </summary>
    Search = 2048,

    /// <summary>
    /// A tag (keyword) for the current document.
    /// </summary>
    Tag = 4096
}
