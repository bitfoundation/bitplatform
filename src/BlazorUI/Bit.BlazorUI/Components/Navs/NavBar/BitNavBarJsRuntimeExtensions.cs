namespace Bit.BlazorUI;

internal static class BitNavBarJsRuntimeExtensions
{
    // The selected item is looked up on the JS side rather than handed over as an element, so the scroll
    // does not depend on an element reference the navbar may not have been handed yet.
    internal static ValueTask BitNavBarScrollSelectedItemIntoView(this IJSRuntime jsRuntime, string containerId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.NavBar.scrollItemIntoView", containerId, null);
    }

    internal static ValueTask BitNavBarScrollItemIntoView(this IJSRuntime jsRuntime, string containerId, ElementReference item)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.NavBar.scrollItemIntoView", containerId, item);
    }
}
