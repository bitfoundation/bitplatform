namespace Bit.BlazorUI;

internal static class BitPdfReaderJsInterop
{
    public static ValueTask InitPdfJs(this IJSRuntime jsRuntime, IEnumerable<string> scripts)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.initPdfJs", scripts);
    }

    public static ValueTask SetupPdf(this IJSRuntime jsRuntime, string id, string url)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.BitPdfReader.setupPdf", id, url);
    }
}
