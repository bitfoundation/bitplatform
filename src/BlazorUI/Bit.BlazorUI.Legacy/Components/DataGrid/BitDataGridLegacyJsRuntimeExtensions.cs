namespace Bit.BlazorUI.Legacy;

internal static class BitDataGridJsRuntimeExtensions
{
    public static async ValueTask<IJSObjectReference> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.Legacy.DataGrid.init", tableElement);
    }

    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Legacy.DataGrid.checkColumnOptionsPosition", tableElement);
    }
}
