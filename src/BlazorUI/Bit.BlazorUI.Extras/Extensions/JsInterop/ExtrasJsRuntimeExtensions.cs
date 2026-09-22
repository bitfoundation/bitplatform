namespace Bit.BlazorUI;

internal static class ExtrasJsRuntimeExtensions
{
    internal static ValueTask BitExtrasApplyRootClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.applyRootClasses", cssClasses, cssVariables);
    }

    internal static ValueTask BitExtrasCopyToClipboard(this IJSRuntime jsRuntime, string text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.copyToClipboard", text);
    }

    internal static ValueTask BitExtrasGoToTop(this IJSRuntime jsRuntime, ElementReference element, BitScrollBehavior? behavior = null)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.goToTop", element, behavior?.ToString().ToLowerInvariant());
    }

    internal static ValueTask BitExtrasGoToBottom(this IJSRuntime jsRuntime, ElementReference element, BitScrollBehavior? behavior = null)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.goToBottom", element, behavior?.ToString().ToLowerInvariant());
    }

    internal static ValueTask BitExtrasScrollTo(this IJSRuntime jsRuntime, ElementReference element, double? left, double? top, BitScrollBehavior? behavior = null)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.scrollTo", element, left, top, behavior?.ToString().ToLowerInvariant());
    }

    internal static ValueTask BitExtrasScrollBy(this IJSRuntime jsRuntime, ElementReference element, double x, double y, BitScrollBehavior? behavior = null)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.scrollBy", element, x, y, behavior?.ToString().ToLowerInvariant());
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
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.setPreventKeys", element, keys);
    }

    internal static ValueTask BitExtrasSetPreventKeys(this IJSRuntime jsRuntime, ElementReference element, string[] keys, string targetSelector, string scopeSelector)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.setPreventKeys", element, keys, targetSelector, scopeSelector);
    }

    internal static ValueTask<int[]?> BitExtrasGetElementsOrder(this IJSRuntime jsRuntime, ElementReference[] elements)
    {
        return jsRuntime.Invoke<int[]?>("BitBlazorUI.Extras.getElementsOrder", elements);
    }

    internal static ValueTask BitExtrasScrollIntoView(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Extras.scrollIntoView", element);
    }

    internal static ValueTask BitExtrasDisposePreventKeys(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.disposePreventKeys", element);
    }

    internal static ValueTask BitExtrasSetInputValue(this IJSRuntime jsRuntime, ElementReference element, string? value)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.setInputValue", element, value);
    }

    internal static ValueTask BitExtrasScrollElementIntoView(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Extras.scrollElementIntoView", elementId);
    }
}
