using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class ExtrasJsRuntimeExtensions
{
    internal static ValueTask BitExtrasApplyRootClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.applyRootClasses", cssClasses, cssVariables);
    }

    internal static ValueTask BitExtrasGoToTop(this IJSRuntime jsRuntime, ElementReference element, BitScrollBehavior? behavior = null)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.goToTop", element, behavior?.ToString().ToLowerInvariant());
    }

    internal static ValueTask BitExtrasScrollBy(this IJSRuntime jsRuntime, ElementReference element, decimal x, decimal y)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.scrollBy", element, x, y);
    }
}
