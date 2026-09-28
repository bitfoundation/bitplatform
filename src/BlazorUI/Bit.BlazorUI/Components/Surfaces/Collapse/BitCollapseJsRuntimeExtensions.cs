namespace Bit.BlazorUI;

internal static class BitCollapseJsRuntimeExtensions
{
    internal static ValueTask<double?> BitCollapseGetTransitionTime(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.Invoke<double?>("BitBlazorUI.Collapse.getTransitionTime", root);
    }
}
