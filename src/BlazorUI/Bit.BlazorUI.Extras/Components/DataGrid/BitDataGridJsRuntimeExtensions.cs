using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitDataGridJsRuntimeExtensions
{
    public static async ValueTask<IJSObjectReference> BitDataGridInit(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        return await jsRuntime.FastInvoke<IJSObjectReference>("BitBlazorUI.DataGrid.init", tableElement);
    }

    public static async ValueTask BitDataGridCheckColumnOptionsPosition(this IJSRuntime jsRuntime, ElementReference tableElement)
    {
        await jsRuntime.FastInvokeVoid("BitBlazorUI.DataGrid.checkColumnOptionsPosition", tableElement);
    }
}
