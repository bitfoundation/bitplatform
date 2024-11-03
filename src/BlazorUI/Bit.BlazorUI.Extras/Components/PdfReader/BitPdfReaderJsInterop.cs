namespace Bit.BlazorUI;

internal static class BitPdfReaderJsInterop
{
    public static ValueTask InitPdfJs(this IJSRuntime jsRuntime, IEnumerable<string> scripts)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.init", scripts);
    }

    public static ValueTask SetupPdfJs(this IJSRuntime jsRuntime, BitPdfReaderConfig config)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.setup", config);
    }

    public static ValueTask renderPdfJsPage(this IJSRuntime jsRuntime, string id, int pageNumber)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.renderPage", id, pageNumber);
    }
}
