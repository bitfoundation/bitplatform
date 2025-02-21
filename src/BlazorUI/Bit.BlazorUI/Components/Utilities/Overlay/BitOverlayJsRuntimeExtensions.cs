using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitOverlayJsRuntimeExtensions
{
    internal static ValueTask<int> BitOverlayToggleScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isOpen)
    {
        return jsRuntime.FastInvoke<int>("BitBlazorUI.Overlay.toggleScroll", scrollerSelector, isOpen);
    }
}
