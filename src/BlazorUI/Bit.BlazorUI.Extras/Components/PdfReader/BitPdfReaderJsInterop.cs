namespace Bit.BlazorUI;

internal static class BitPdfReaderJsInterop
{
    public static ValueTask BitPdfReaderInitPdfJs(this IJSRuntime jsRuntime, IEnumerable<string> scripts)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.init", scripts);
    }

    public static ValueTask<int> BitPdfReaderSetupPdfDoc(this IJSRuntime jsRuntime, BitPdfReaderConfig config)
    {
        return jsRuntime.InvokeAsync<int>("BitBlazorUI.BitPdfReader.setup", config);
    }

    public static ValueTask BitPdfReaderRenderPage(this IJSRuntime jsRuntime, string id, int pageNumber)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.renderPage", id, pageNumber);
    }

    public static ValueTask BitPdfReaderRefreshPage(this IJSRuntime jsRuntime, BitPdfReaderConfig config, int pageNumber)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.refreshPage", config, pageNumber);
    }
}
