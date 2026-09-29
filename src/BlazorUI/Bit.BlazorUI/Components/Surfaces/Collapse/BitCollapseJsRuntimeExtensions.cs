namespace Bit.BlazorUI;

internal static class BitCollapseJsRuntimeExtensions
{
    internal static ValueTask<double?> BitCollapseGetRemainingTransitionTime(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.Invoke<double?>("BitBlazorUI.Collapse.getRemainingTransitionTime", root);
    }
}
