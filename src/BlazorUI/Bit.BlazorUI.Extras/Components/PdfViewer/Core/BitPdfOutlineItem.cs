// Document outline (bookmarks) parsing. Resolves the /Outlines tree and maps
// each item's destination to a page index.

namespace Bit.BlazorUI;

/// <summary>A single entry in the document outline (a bookmark).</summary>
public sealed class BitPdfOutlineItem
{
    /// <summary>The bookmark label.</summary>
    public string Title { get; init; } = "";

    /// <summary>The target page (1-based), or <c>null</c> if it could not be resolved.</summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// The full resolved destination (page plus view parameters), or <c>null</c>
    /// when the bookmark has no usable destination. <see cref="PageNumber"/> is a
    /// convenience shortcut for <c>Destination?.PageNumber</c>.
    /// </summary>
    public BitPdfDestination? Destination { get; init; }

    /// <summary>Nested child bookmarks.</summary>
    public IReadOnlyList<BitPdfOutlineItem> Children { get; init; } = Array.Empty<BitPdfOutlineItem>();
}
