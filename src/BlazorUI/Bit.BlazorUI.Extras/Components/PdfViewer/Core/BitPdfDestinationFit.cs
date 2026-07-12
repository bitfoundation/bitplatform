// Explicit-destination parsing and normalization.
// A destination array is [pageRef /Type args...]; see PDF 32000-1:2008 §12.3.2.

namespace Bit.BlazorUI;

/// <summary>The view-fit modes a destination can request (PDF §12.3.2.2).</summary>
public enum BitPdfDestinationFit
{
    /// <summary>The destination array could not be interpreted.</summary>
    Unknown,

    /// <summary>[/XYZ left top zoom] - position the given point with a zoom level.</summary>
    XYZ,

    /// <summary>[/Fit] - fit the whole page in the window.</summary>
    Fit,

    /// <summary>[/FitH top] - fit the page width, with the given top edge.</summary>
    FitH,

    /// <summary>[/FitV left] - fit the page height, with the given left edge.</summary>
    FitV,

    /// <summary>[/FitR left bottom right top] - fit the given rectangle.</summary>
    FitR,

    /// <summary>[/FitB] - fit the page's bounding box.</summary>
    FitB,

    /// <summary>[/FitBH top] - fit the bounding-box width.</summary>
    FitBH,

    /// <summary>[/FitBV left] - fit the bounding-box height.</summary>
    FitBV,
}
