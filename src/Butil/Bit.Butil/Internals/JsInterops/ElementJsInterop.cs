using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class ElementJsInterop
{
    internal static async Task ElementBlur(this IJSRuntime js, ElementReference element)
    {
        await js.InvokeVoidAsync("BitButil.element.blur", element);
    }

    internal static async Task<string> ElementGetAttribute(this IJSRuntime js, ElementReference element, string name)
    {
        return await js.InvokeAsync<string>("BitButil.element.getAttribute", element, name);
    }

    internal static async Task<string[]> ElementGetAttributeNames(this IJSRuntime js, ElementReference element)
    {
        return await js.InvokeAsync<string[]>("BitButil.element.getAttribute", element);
    }

    internal static async Task<DomRect> ElementGetBoundingClientRect(this IJSRuntime js, ElementReference element)
    {
        return await js.InvokeAsync<DomRect>("BitButil.element.getBoundingClientRect", element);
    }

    internal static async Task ElementReleasePointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
    {
        await js.InvokeVoidAsync("BitButil.element.releasePointerCapture", element, pointerId);
    }

    internal static async Task ElementRemove(this IJSRuntime js, ElementReference element)
    {
        await js.InvokeVoidAsync("BitButil.element.remove", element);
    }

    internal static async Task<string> ElementRemoveAttribute(this IJSRuntime js, ElementReference element, string name)
    {
        return await js.InvokeAsync<string>("BitButil.element.removeAttribute", element, name);
    }

    internal static async Task ElementRequestFullScreen(this IJSRuntime js, ElementReference element, FullScreenOptions? options)
    {
        await js.InvokeVoidAsync("BitButil.element.requestFullScreen", element, options);
    }

    internal static async Task ElementScroll(this IJSRuntime js, ElementReference element, ScrollToOptions? options, double? x, double? y)
    {
        await js.InvokeVoidAsync("BitButil.element.scroll", element, options, x, y);
    }

    internal static async Task ElementScrollBy(this IJSRuntime js, ElementReference element, ScrollToOptions? options, double? x, double? y)
    {
        await js.InvokeVoidAsync("BitButil.element.scrollBy", element, options, x, y);
    }

    internal static async Task ElementScrollIntoView(this IJSRuntime js, ElementReference element, bool? alignToTop, ScrollIntoViewOptions? options)
    {
        await js.InvokeVoidAsync("BitButil.element.scrollIntoView", element, alignToTop, options);
    }

    internal static async Task ElementSetAttribute(this IJSRuntime js, ElementReference element, string name, string value)
    {
        await js.InvokeVoidAsync("BitButil.element.setAttribute", element, name, value);
    }

    internal static async Task ElementSetPointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
    {
        await js.InvokeVoidAsync("BitButil.element.setPointerCapture", element, pointerId);
    }
}
