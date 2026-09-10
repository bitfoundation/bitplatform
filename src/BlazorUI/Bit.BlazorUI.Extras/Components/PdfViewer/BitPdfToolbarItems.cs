namespace Bit.BlazorUI;

/// <summary>
/// The individually toggleable groups of controls in the <see cref="BitPdfViewer"/>
/// toolbar. Combine the members to build a reduced toolbar, e.g.
/// <c>BitPdfToolbarItems.Navigation | BitPdfToolbarItems.Zoom</c>.
/// </summary>
[Flags]
public enum BitPdfToolbarItems
{
    /// <summary>No toolbar control at all (the toolbar bar itself still renders).</summary>
    None = 0,

    /// <summary>The page-thumbnails sidebar toggle.</summary>
    Thumbnails = 1,

    /// <summary>The bookmarks (document outline) sidebar toggle.</summary>
    Bookmarks = 2,

    /// <summary>The previous/next page buttons and the page-number box.</summary>
    Navigation = 4,

    /// <summary>The first/last page buttons.</summary>
    FirstLastPage = 8,

    /// <summary>The zoom in/out buttons and the zoom-level dropdown.</summary>
    Zoom = 16,

    /// <summary>The document title.</summary>
    Title = 32,

    /// <summary>The find-in-document control.</summary>
    Search = 64,

    /// <summary>The rotate clockwise/counter-clockwise buttons.</summary>
    Rotate = 128,

    /// <summary>The download button.</summary>
    Download = 256,

    /// <summary>The print button.</summary>
    Print = 512,

    /// <summary>The fullscreen toggle.</summary>
    Fullscreen = 1024,

    /// <summary>The document-properties button and its dialog.</summary>
    Properties = 2048,

    /// <summary>The scroll-mode and spread-mode dropdowns.</summary>
    Layout = 4096,

    /// <summary>The pan (hand) tool toggle.</summary>
    CursorTool = 8192,

    /// <summary>The embedded-files (attachments) sidebar toggle.</summary>
    Attachments = 16384,

    /// <summary>The presentation-mode toggle.</summary>
    Presentation = 32768,

    /// <summary>The optional-content (layers) sidebar toggle.</summary>
    Layers = 65536,

    /// <summary>Every toolbar control (the default).</summary>
    All = Thumbnails | Bookmarks | Navigation | FirstLastPage | Zoom | Title
        | Search | Rotate | Download | Print | Fullscreen | Properties | Layout | CursorTool
        | Attachments | Presentation | Layers,
}
