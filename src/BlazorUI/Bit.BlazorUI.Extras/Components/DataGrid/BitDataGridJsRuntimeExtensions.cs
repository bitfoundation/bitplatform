namespace Bit.BlazorUI;

internal static class BitDataGridJsRuntimeExtensions
{
    // FastInvoke can return null when the in-process (WASM) path swallows a JSON error (it returns
    // default), so the contract is nullable. Callers must null-check before using the reference.
    public static async ValueTask<IJSObjectReference?> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.FastInvoke<IJSObjectReference>("BitBlazorUI.DataGrid.init", tableElement);
    }

    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.FastInvokeVoid("BitBlazorUI.DataGrid.checkColumnOptionsPosition", tableElement);
    }
}
