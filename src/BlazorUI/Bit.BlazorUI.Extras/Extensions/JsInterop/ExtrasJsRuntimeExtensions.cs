namespace Bit.BlazorUI;

internal static class ExtrasJsRuntimeExtensions
{
    internal static ValueTask BitExtrasApplyRootClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.applyRootClasses", cssClasses, cssVariables);
    }

    internal static ValueTask BitExtrasGoToTop(this IJSRuntime jsRuntime, ElementReference element, BitScrollBehavior? behavior = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.goToTop", element, behavior?.ToString().ToLowerInvariant());
    }

    internal static ValueTask BitExtrasScrollBy(this IJSRuntime jsRuntime, ElementReference element, decimal x, decimal y)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.scrollBy", element, x, y);
    }

    public static ValueTask BitExtrasInitScripts(this IJSRuntime jsRuntime, IEnumerable<string> scripts, bool isModule = false)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.initScripts", scripts, isModule);
    }

    public static ValueTask BitExtrasInitStylesheets(this IJSRuntime jsRuntime, IEnumerable<string> stylesheets)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.initStylesheets", stylesheets);
    }

    internal static ValueTask BitExtrasSetPreventKeys(this IJSRuntime jsRuntime, ElementReference element, string[] keys)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.setPreventKeys", element, keys);
    }

    internal static ValueTask BitExtrasDisposePreventKeys(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.disposePreventKeys", element);
    }

    internal static ValueTask BitExtrasScrollOptionIntoView(this IJSRuntime jsRuntime, string optionId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.scrollOptionIntoView", optionId);
    }
}
