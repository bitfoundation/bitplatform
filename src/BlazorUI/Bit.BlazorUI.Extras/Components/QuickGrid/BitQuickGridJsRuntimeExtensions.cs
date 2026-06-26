namespace Bit.BlazorUI;

internal static class BitQuickGridJsRuntimeExtensions
{
    public static async ValueTask<IJSObjectReference> BitQuickGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.QuickGrid.init", tableElement);
    }

    public static async ValueTask BitQuickGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.QuickGrid.checkColumnOptionsPosition", tableElement);
    }
}
