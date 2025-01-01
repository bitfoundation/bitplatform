namespace Bit.BlazorUI;

internal static class BitExtrasJsRuntimeExtensions
{
    internal static ValueTask BitExtrasApplyRootClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitExtras.applyRootClasses", cssClasses, cssVariables);
    }

    internal static ValueTask BitExtrasGoToTop(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitExtras.goToTop", element);
    }

    internal static ValueTask BitExtrasScrollBy(this IJSRuntime jsRuntime, ElementReference element, decimal x, decimal y)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.BitExtras.scrollBy", element, x, y);
    }
}
