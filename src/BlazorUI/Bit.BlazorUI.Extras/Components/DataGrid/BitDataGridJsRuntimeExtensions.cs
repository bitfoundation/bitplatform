﻿namespace Bit.BlazorUI;

internal static class BitDataGridJsRuntimeExtensions
{
    public static async ValueTask<IJSObjectReference> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.BitDataGrid.init", tableElement);
    }

    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.BitDataGrid.checkColumnOptionsPosition", tableElement);
    }
}
