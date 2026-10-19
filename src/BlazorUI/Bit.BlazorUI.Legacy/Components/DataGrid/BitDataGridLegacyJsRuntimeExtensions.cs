namespace Bit.BlazorUI.Legacy;

internal static class BitDataGridLegacyJsRuntimeExtensions
{
    public static async ValueTask<IJSObjectReference> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.InvokeScriptAsync<IJSObjectReference>("BitBlazorUI.Legacy.DataGrid.init", tableElement);
    }

    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.InvokeScriptVoidAsync("BitBlazorUI.Legacy.DataGrid.checkColumnOptionsPosition", tableElement);
    }
}
