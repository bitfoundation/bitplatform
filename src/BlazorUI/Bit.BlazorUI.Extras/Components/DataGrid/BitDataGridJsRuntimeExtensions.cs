namespace Bit.BlazorUI;

internal static class BitDataGridJsRuntimeExtensions
{
    public static async ValueTask<IJSObjectReference> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.Invoke<IJSObjectReference>("BitBlazorUI.DataGrid.init", tableElement);
    }

    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.InvokeVoid("BitBlazorUI.DataGrid.checkColumnOptionsPosition", tableElement);
    }
}
