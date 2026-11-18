namespace Bit.BbScanner;

/// <summary>
/// Where the scanner's video element is, for a native preview drawn behind the page: in CSS pixels of the page's
/// viewport, which a WebView lays out one to one with the platform's device independent units.
/// </summary>
/// <param name="X">The element's left edge.</param>
/// <param name="Y">The element's top edge.</param>
/// <param name="Width">The element's width.</param>
/// <param name="Height">The element's height.</param>
/// <param name="CornerRadius">The scanner's corner radius, so the preview is rounded as the scanner is.</param>
/// <param name="Visible">Whether the element is on screen at all.</param>
/// <param name="Backdrop">
/// The page's own background colour as CSS gives it: the page is made transparent around the scanner while a preview
/// shows through it, and what is behind the page is painted this colour instead.
/// </param>
/// <param name="ClipX">The left edge of the part of the viewport the element shows in.</param>
/// <param name="ClipY">The top edge of that part.</param>
/// <param name="ClipWidth">Its width.</param>
/// <param name="ClipHeight">
/// Its height. The viewport, cut down by every element above the scanner that clips what overflows it - a scrolling
/// panel the scanner is partly scrolled out of, say: the preview shows inside it only.
/// </param>
/// <remarks>The default placement says the page has taken the preview out altogether: put everything back.</remarks>
public readonly record struct BbScannerPreviewPlacement(
    double X, double Y, double Width, double Height, double CornerRadius, bool Visible, string? Backdrop,
    double ClipX, double ClipY, double ClipWidth, double ClipHeight)
{
    /// <summary>Whether this is the placement that says the page has taken the preview out.</summary>
    public bool IsDetached => this == default;
}
