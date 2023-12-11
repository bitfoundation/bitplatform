using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class ElementJsInterop
{
    internal static async Task ElementBlur(this IJSRuntime js, ElementReference element)
        => await js.InvokeVoidAsync("BitButil.element.blur", element);

    internal static async Task<string> ElementGetAttribute(this IJSRuntime js, ElementReference element, string name)
        => await js.InvokeAsync<string>("BitButil.element.getAttribute", element, name);

    internal static async Task<string[]> ElementGetAttributeNames(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string[]>("BitButil.element.getAttribute", element);

    internal static async Task<DomRect> ElementGetBoundingClientRect(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<DomRect>("BitButil.element.getBoundingClientRect", element);

    internal static async Task ElementReleasePointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
        => await js.InvokeVoidAsync("BitButil.element.releasePointerCapture", element, pointerId);

    internal static async Task ElementRemove(this IJSRuntime js, ElementReference element)
        => await js.InvokeVoidAsync("BitButil.element.remove", element);

    internal static async Task<string> ElementRemoveAttribute(this IJSRuntime js, ElementReference element, string name)
        => await js.InvokeAsync<string>("BitButil.element.removeAttribute", element, name);

    internal static async Task ElementRequestFullScreen(this IJSRuntime js, ElementReference element, FullScreenOptions? options)
        => await js.InvokeVoidAsync("BitButil.element.requestFullScreen", element, options);

    internal static async Task ElementScroll(this IJSRuntime js, ElementReference element, ScrollToOptions? options, double? x, double? y)
        => await js.InvokeVoidAsync("BitButil.element.scroll", element, options, x, y);

    internal static async Task ElementScrollBy(this IJSRuntime js, ElementReference element, ScrollToOptions? options, double? x, double? y)
        => await js.InvokeVoidAsync("BitButil.element.scrollBy", element, options, x, y);

    internal static async Task ElementScrollIntoView(this IJSRuntime js, ElementReference element, bool? alignToTop, ScrollIntoViewOptions? options)
        => await js.InvokeVoidAsync("BitButil.element.scrollIntoView", element, alignToTop, options);

    internal static async Task ElementSetAttribute(this IJSRuntime js, ElementReference element, string name, string value)
        => await js.InvokeVoidAsync("BitButil.element.setAttribute", element, name, value);

    internal static async Task ElementSetPointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
        => await js.InvokeVoidAsync("BitButil.element.setPointerCapture", element, pointerId);

    internal static async Task<string> ElementGetAccessKey(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getAccessKey", element);

    internal static async Task ElementSetAccessKey(this IJSRuntime js, ElementReference element, string key)
        => await js.InvokeVoidAsync("BitButil.element.setAccessKey", element, key);

    internal static async Task<string> ElementGetClassName(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getClassName", element);

    internal static async Task ElementSetClassName(this IJSRuntime js, ElementReference element, string className)
        => await js.InvokeVoidAsync("BitButil.element.setClassName", element, className);

    internal static async Task<float> ElementGetClientHeight(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientHeight", element);

    internal static async Task<float> ElementGetClientLeft(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientLeft", element);

    internal static async Task<float> ElementGetClientTop(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientTop", element);

    internal static async Task<float> ElementGetClientWidth(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientWidth", element);

    internal static async Task<string> ElementGetId(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getId", element);

    internal static async Task ElementSetId(this IJSRuntime js, ElementReference element, string id)
        => await js.InvokeVoidAsync("BitButil.element.setId", element, id);

    internal static async Task<string> ElementGetInnerHTML(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getInnerHTML", element);

    internal static async Task ElementSetInnerHTML(this IJSRuntime js, ElementReference element, string innerHTML)
        => await js.InvokeVoidAsync("BitButil.element.setInnerHTML", element, innerHTML);

    internal static async Task<string> ElementGetOuterHTML(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getOuterHTML", element);

    internal static async Task ElementSetOuterHTML(this IJSRuntime js, ElementReference element, string outerHTML)
        => await js.InvokeVoidAsync("BitButil.element.setOuterHTML", element, outerHTML);

    internal static async Task<float> ElementGetScrollHeight(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollHeight", element);
}
