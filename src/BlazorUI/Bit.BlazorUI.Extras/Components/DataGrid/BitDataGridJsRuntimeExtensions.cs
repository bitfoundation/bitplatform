namespace Bit.BlazorUI;

internal static class BitDataGridJsRuntimeExtensions
{
    // FastInvoke returns default (null) when the runtime can't service interop or a JSON/JS interop
    // error is swallowed on the in-process (WASM) path. Callers must null-check before using the
    // reference; a null result means DataGrid JS hooks were not initialized.
    public static async ValueTask<IJSObjectReference?> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        const string identifier = "BitBlazorUI.DataGrid.init";
        var result = await jsRuntime.FastInvoke<IJSObjectReference>(identifier, tableElement);
        return jsRuntime.ReportIfUnexpectedNull(identifier, result);
    }

    // This is a fire-and-forget call from OnAfterRenderAsync that runs DOM-heavy positioning logic
    // (getBoundingClientRect, scrollIntoViewIfNeeded, focus). It deliberately uses the regular async
    // invocation rather than FastInvokeVoid: on WebAssembly FastInvokeVoid runs synchronously and can
    // alter Promise/ordering and error-propagation semantics, so we use the async Invoke pattern to keep
    // any JS-side failure (e.g. scrollIntoViewIfNeeded being unsupported) contained within the returned
    // task instead of letting it escape synchronously into the render loop.
    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.InvokeVoid("BitBlazorUI.DataGrid.checkColumnOptionsPosition", tableElement);
    }
}
