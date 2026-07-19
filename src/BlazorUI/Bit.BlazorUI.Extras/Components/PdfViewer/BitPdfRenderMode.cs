namespace Bit.BlazorUI;

/// <summary>How page content is rendered.</summary>
public enum BitPdfRenderMode
{
    /// <summary>
    /// Pages render to positioned HTML/CSS DOM (the default). Fully
    /// prerenderable (server-side rendering ships the final pixels' markup)
    /// and crisp at any zoom.
    /// </summary>
    Html,

    /// <summary>
    /// Page content is painted onto a per-page <c>&lt;canvas&gt;</c> by replaying
    /// a display list produced by the C# engine (the pdf.js model). Far fewer DOM
    /// nodes; selection, search and links still work through the DOM text layer,
    /// and zoom changes re-rasterize the canvases so text stays crisp. Requires
    /// JavaScript - a prerendered page shows a blank canvas until the client
    /// loads.
    /// </summary>
    Canvas,
}
