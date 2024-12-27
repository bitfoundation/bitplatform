namespace Bit.BlazorUI;

internal static class BitExtrasJsRuntimeExtensions
{
    internal static ValueTask BitExtrasApplyRootClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitExtras.applyRootClasses", cssClasses, cssVariables);
    }
}
