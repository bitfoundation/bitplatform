namespace Bit.BlazorUI;

internal static class BitDataGridJsRuntimeExtensions
{
    // FastInvoke can return null when the in-process (WASM) path swallows a JSON error (it returns
    // default), so the contract is nullable. Callers must null-check before using the reference.
    public static async ValueTask<IJSObjectReference?> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.FastInvoke<IJSObjectReference>("BitBlazorUI.DataGrid.init", tableElement);
    }

    // This is a fire-and-forget call from OnAfterRenderAsync that runs DOM-heavy positioning logic
    // (getBoundingClientRect, scrollIntoViewIfNeeded, focus). It deliberately uses the regular async
    // invocation rather than FastInvokeVoid: on WebAssembly FastInvokeVoid runs synchronously and only
    // swallows JsonException, so a JS-side failure (e.g. scrollIntoViewIfNeeded being unsupported) would
    // throw synchronously and escape the discarded task into the render loop. The async path keeps any
    // such failure contained within the returned task instead.
    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.InvokeVoid("BitBlazorUI.DataGrid.checkColumnOptionsPosition", tableElement);
    }
}
