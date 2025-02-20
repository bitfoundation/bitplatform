using System.Diagnostics.CodeAnalysis;

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

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static ValueTask BitPdfReaderDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.PdfReader.dispose", id);
    }
}
