namespace Bit.BlazorUI;

internal static class BitPdfReaderJsRuntimeExtensions
{
    public static ValueTask<int> BitPdfReaderSetup(this IJSRuntime jsRuntime, BitPdfReaderConfig config)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.PdfReader.setup", config);
    }

    public static ValueTask BitPdfReaderRenderPage(this IJSRuntime jsRuntime, string id, int pageNumber)
    {
        // The JS renderPage is async (awaits pdf.js page rendering). FastInvoke would use the
        // synchronous in-process path in WASM, discarding the returned Promise (fire-and-forget),
        // so callers would proceed/raise events before rendering completes and errors would be lost.
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfReader.renderPage", id, pageNumber);
    }

    public static ValueTask BitPdfReaderRefreshPage(this IJSRuntime jsRuntime, BitPdfReaderConfig config, int pageNumber)
    {
        // The JS refreshPage is async (awaits renderPage). See BitPdfReaderRenderPage for why
        // the asynchronous invocation must be used instead of the synchronous fast-invoke.
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfReader.refreshPage", config, pageNumber);
    }

    public static ValueTask BitPdfReaderDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.PdfReader.dispose", id);
    }
}
