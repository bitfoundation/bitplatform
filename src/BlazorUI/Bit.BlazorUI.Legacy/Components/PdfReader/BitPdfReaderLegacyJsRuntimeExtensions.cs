namespace Bit.BlazorUI.Legacy;

internal static class BitPdfReaderLegacyJsRuntimeExtensions
{
    public static ValueTask<int> BitPdfReaderSetup(this IJSRuntime jsRuntime, BitPdfReaderLegacyConfig config)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.Legacy.PdfReader.setup", config);
    }

    public static ValueTask BitPdfReaderRenderPage(this IJSRuntime jsRuntime, string id, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.PdfReader.renderPage", id, pageNumber);
    }

    public static ValueTask BitPdfReaderRefreshPage(this IJSRuntime jsRuntime, BitPdfReaderLegacyConfig config, int pageNumber)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.PdfReader.refreshPage", config, pageNumber);
    }

    public static ValueTask BitPdfReaderDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Legacy.PdfReader.dispose", id);
    }
}
