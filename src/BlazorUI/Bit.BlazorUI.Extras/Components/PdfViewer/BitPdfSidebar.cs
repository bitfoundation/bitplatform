namespace Bit.BlazorUI;

/// <summary>Which side panel of the <see cref="BitPdfViewer"/> is open.</summary>
public enum BitPdfSidebar
{
    /// <summary>No side panel is open; the document surface fills the viewer.</summary>
    None,

    /// <summary>The page-thumbnails panel is open.</summary>
    Thumbnails,

    /// <summary>The bookmarks (document outline) panel is open.</summary>
    Bookmarks,

    /// <summary>The embedded-files (attachments) panel is open.</summary>
    Attachments,

    /// <summary>The optional-content (layers) panel is open.</summary>
    Layers,
}
