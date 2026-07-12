namespace Bit.BlazorUI;

internal static class BitPdfViewerJsRuntimeExtensions
{
    public static ValueTask<BitPdfViewerViewport> BitPdfViewerGetViewport(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.Invoke<BitPdfViewerViewport>("BitBlazorUI.PdfViewer.getViewport", container);
    }

    public static ValueTask BitPdfViewerScrollToPage(this IJSRuntime jsRuntime, ElementReference container, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.scrollToPage", container, pageNumber);
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

    public static ValueTask BitPdfViewerPrint(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.print", container);
    }

    public static ValueTask BitPdfViewerPaintCanvasPages(this IJSRuntime jsRuntime, ElementReference container, BitPdfViewerCanvasPage[] pages, double scale)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.paintCanvasPages", container, pages, scale);
    }

    public static ValueTask BitPdfViewerRezoomCanvases(this IJSRuntime jsRuntime, ElementReference container, double scale)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.rezoomCanvases", container, scale);
    }

    public static ValueTask<int> BitPdfViewerSearchAll(this IJSRuntime jsRuntime, ElementReference container, string query)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.PdfViewer.searchAll", container, query);
    }

    public static ValueTask BitPdfViewerGotoMatch(this IJSRuntime jsRuntime, ElementReference container, int index)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfViewer.gotoMatch", container, index);
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
