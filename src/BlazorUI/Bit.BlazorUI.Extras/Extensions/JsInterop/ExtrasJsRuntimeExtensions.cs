using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class ExtrasJsRuntimeExtensions
{
    internal static ValueTask BitExtrasApplyRootClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.applyRootClasses", cssClasses, cssVariables);
    }

    [RequiresUnreferencedCode("Calls Bit.BlazorUI.IJSRuntimeExtensions.FastInvokeVoid(String, params Object[])")]
    internal static ValueTask BitExtrasGoToTop(this IJSRuntime jsRuntime, ElementReference element, BitScrollBehavior? behavior = null)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.goToTop", element, behavior?.ToString().ToLowerInvariant());
    }

    internal static ValueTask BitExtrasScrollBy(this IJSRuntime jsRuntime, ElementReference element, decimal x, decimal y)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.scrollBy", element, x, y);
    }
}
