namespace Bit.BlazorUI;

/// <summary>How the viewer scales pages to the available space.</summary>
public enum BitPdfZoomMode
{
    /// <summary>An explicit zoom factor is applied (the user picked a percentage).</summary>
    Custom,

    /// <summary>Each page is scaled so its width fills the viewport.</summary>
    FitWidth,

    /// <summary>Each page is scaled so the whole page fits in the viewport.</summary>
    FitPage,

    /// <summary>Pages are shown at their natural size (one CSS pixel per point).</summary>
    ActualSize,
}
