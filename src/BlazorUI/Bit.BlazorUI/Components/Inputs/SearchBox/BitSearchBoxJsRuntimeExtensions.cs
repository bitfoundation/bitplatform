using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitSearchBoxJsRuntimeExtensions
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    internal static ValueTask BitSearchBoxMoveCursorToEnd(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.SearchBox.moveCursorToEnd", input);
    }
}
