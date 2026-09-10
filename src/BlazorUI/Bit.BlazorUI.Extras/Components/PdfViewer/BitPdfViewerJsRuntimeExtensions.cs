namespace Bit.BlazorUI;

internal static class BitPdfViewerJsRuntimeExtensions
{
    public static ValueTask<BitPdfViewerViewport> BitPdfViewerGetViewport(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.Invoke<BitPdfViewerViewport>("BitBlazorUI.PdfViewer.getViewport", container);
    }

    public static ValueTask<string> BitPdfViewerGetSelectedText(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.PdfViewer.getSelectedText", container);
    }

    public static ValueTask BitPdfViewerClearSelection(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.clearSelection", container);
    }

    public static ValueTask BitPdfViewerScrollToPage(this IJSRuntime jsRuntime, ElementReference container, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.scrollToPage", container, pageNumber);
    }

    public static ValueTask BitPdfViewerScrollToPageOffset(this IJSRuntime jsRuntime, ElementReference container, int pageNumber, double offset)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.scrollToPageOffset", container, pageNumber, offset);
    }

    public static ValueTask BitPdfViewerRestoreZoomAnchor(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.restoreZoomAnchor", container);
    }

    public static ValueTask BitPdfViewerRegisterScrollSpy(this IJSRuntime jsRuntime, ElementReference container, DotNetObjectReference<BitPdfViewer> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.registerScrollSpy", container, dotnetObj);
    }

    public static ValueTask BitPdfViewerDisposeScrollSpy(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.disposeScrollSpy", container);
    }

    public static ValueTask BitPdfViewerRegisterThumbSpy(this IJSRuntime jsRuntime, ElementReference container, DotNetObjectReference<BitPdfViewer> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.registerThumbSpy", container, dotnetObj);
    }

    public static ValueTask BitPdfViewerDisposeThumbSpy(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.disposeThumbSpy", container);
    }

    public static ValueTask BitPdfViewerRegisterKeyboard(this IJSRuntime jsRuntime, ElementReference root, DotNetObjectReference<BitPdfViewer> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.registerKeyboard", root, dotnetObj);
    }

    public static ValueTask BitPdfViewerDisposeKeyboard(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.disposeKeyboard", root);
    }

    public static ValueTask BitPdfViewerRegisterDropZone(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.registerDropZone", root);
    }

    public static ValueTask BitPdfViewerDisposeDropZone(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.disposeDropZone", root);
    }

    public static ValueTask BitPdfViewerFocus(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.focus", element);
    }

    public static ValueTask BitPdfViewerSetValue(this IJSRuntime jsRuntime, ElementReference element, string value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.setValue", element, value);
    }

    public static ValueTask BitPdfViewerTrapFocus(this IJSRuntime jsRuntime, ElementReference dialog)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.trapFocus", dialog);
    }

    public static ValueTask BitPdfViewerReleaseFocus(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.releaseFocus");
    }

    public static ValueTask BitPdfViewerFocusThumb(this IJSRuntime jsRuntime, ElementReference container, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.focusThumb", container, pageNumber);
    }

    public static ValueTask BitPdfViewerFocusOutlineItem(this IJSRuntime jsRuntime, ElementReference root, int index)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.focusOutlineItem", root, index);
    }

    public static ValueTask BitPdfViewerScrollThumbIntoView(this IJSRuntime jsRuntime, ElementReference container, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.scrollThumbIntoView", container, pageNumber);
    }

    public static ValueTask BitPdfViewerDownload(this IJSRuntime jsRuntime, string fileName, DotNetStreamReference streamRef)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.download", fileName, streamRef);
    }

    public static ValueTask BitPdfViewerCorrectTextWidths(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.correctTextWidths", container);
    }

    public static ValueTask BitPdfViewerToggleFullscreen(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.toggleFullscreen", element);
    }

    public static ValueTask BitPdfViewerExitFullscreen(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.exitFullscreen");
    }

    public static ValueTask BitPdfViewerRegisterFullscreenSpy(this IJSRuntime jsRuntime, ElementReference root, DotNetObjectReference<BitPdfViewer> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.registerFullscreenSpy", root, dotnetObj);
    }

    public static ValueTask BitPdfViewerDisposeFullscreenSpy(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.disposeFullscreenSpy", root);
    }

    public static ValueTask BitPdfViewerPrint(this IJSRuntime jsRuntime, ElementReference container, int from, int to)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.print", container, from, to);
    }

    public static ValueTask BitPdfViewerPaintCanvasPages(this IJSRuntime jsRuntime, ElementReference container, BitPdfViewerCanvasPage[] pages, double scale)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.paintCanvasPages", container, pages, scale);
    }

    public static ValueTask BitPdfViewerRezoomCanvases(this IJSRuntime jsRuntime, ElementReference container, double scale)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.rezoomCanvases", container, scale);
    }

    public static ValueTask BitPdfViewerHighlight(this IJSRuntime jsRuntime, ElementReference container, string query,
        bool matchCase, bool wholeWord, bool matchDiacritics, bool highlightAll,
        int currentPage, int currentOrdinal, bool scrollToCurrent)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.highlight", container, query, matchCase, wholeWord,
            matchDiacritics, highlightAll, currentPage, currentOrdinal, scrollToCurrent);
    }

    public static ValueTask BitPdfViewerClearSearch(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.clearSearch", container);
    }
}

/// <summary>
/// The dimensions of the scrollable document surface, reported by JavaScript.
/// </summary>
internal readonly record struct BitPdfViewerViewport(double Width, double Height);

/// <summary>
/// A per-page canvas-mode paint request: the page's display list and its size in points.
/// </summary>
internal sealed class BitPdfViewerCanvasPage
{
    /// <summary>The 1-based page number.</summary>
    public int Page { get; set; }

    /// <summary>The page width in PDF points (display orientation).</summary>
    public double W { get; set; }

    /// <summary>The page height in PDF points (display orientation).</summary>
    public double H { get; set; }

    /// <summary>The JSON display list to replay onto the page's canvas.</summary>
    public string? Ops { get; set; }
}
