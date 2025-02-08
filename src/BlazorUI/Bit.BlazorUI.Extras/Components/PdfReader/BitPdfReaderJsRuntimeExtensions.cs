namespace Bit.BlazorUI;

internal static class BitPdfReaderJsRuntimeExtensions
{
    public static ValueTask BitPdfReaderInit(this IJSRuntime jsRuntime, IEnumerable<string> scripts)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfReader.init", scripts);
    }

    public static ValueTask<int> BitPdfReaderSetup(this IJSRuntime jsRuntime, BitPdfReaderConfig config)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.PdfReader.setup", config);
    }

    public static ValueTask BitPdfReaderRenderPage(this IJSRuntime jsRuntime, string id, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfReader.renderPage", id, pageNumber);
    }

    public static ValueTask BitPdfReaderRefreshPage(this IJSRuntime jsRuntime, BitPdfReaderConfig config, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfReader.refreshPage", config, pageNumber);
    }

    public static ValueTask BitPdfReaderDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PdfReader.dispose", id);
    }
}
