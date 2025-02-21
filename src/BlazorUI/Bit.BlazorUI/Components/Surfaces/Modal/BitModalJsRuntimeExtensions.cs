using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitModalJsRuntimeExtensions
{
    internal static ValueTask BitModalSetupDragDrop(this IJSRuntime jsRuntime, string id, string dragElementSelector)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Modal.setupDragDrop", id, dragElementSelector);
    }

    internal static ValueTask BitModalRemoveDragDrop(this IJSRuntime jsRuntime, string id, string dragElementSelector)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Modal.removeDragDrop", id, dragElementSelector);
    }
}
