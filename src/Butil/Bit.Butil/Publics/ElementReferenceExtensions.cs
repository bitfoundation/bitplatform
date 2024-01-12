using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

public static class ElementReferenceExtensions
{
    //[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_JSRuntime")]
    //extern static IJSRuntime JSRuntimeGetter(WebElementReferenceContext context);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<JSRuntime>k__BackingField")]
    extern static ref IJSRuntime JSRuntimeGetter(WebElementReferenceContext context);

    private static IJSRuntime GetJSRuntime(ElementReference elementReference)
    {
        var context = (elementReference.Context as WebElementReferenceContext) ?? throw new InvalidOperationException("ElementReference has not been configured correctly.");

        return JSRuntimeGetter(context);
    }


    public static ValueTask Blur(this ElementReference element)
        => GetJSRuntime(element).ElementBlur(element);

    public static ValueTask<string> GetAttribute(this ElementReference element, string name)
        => GetJSRuntime(element).ElementGetAttribute(element, name);

    public static async ValueTask<string[]> GetAttributeNames(this ElementReference element)
        => await GetJSRuntime(element).ElementGetAttributeNames(element);

    public static async ValueTask<Rect> GetBoundingClientRect(this ElementReference element)
        => await GetJSRuntime(element).ElementGetBoundingClientRect(element);

    public static async ValueTask<bool> HasAttribute(this ElementReference element, string name)
        => await GetJSRuntime(element).ElementHasAttribute(element, name);

    public static async ValueTask<bool> HasAttributes(this ElementReference element)
        => await GetJSRuntime(element).ElementHasAttributes(element);

    public static async ValueTask<bool> HasPointerCapture(this ElementReference element, int pointerId)
        => await GetJSRuntime(element).ElementHasPointerCapture(element, pointerId);

    public static async ValueTask<bool> Matches(this ElementReference element, string selectors)
        => await GetJSRuntime(element).ElementMatches(element, selectors);

    public static async ValueTask ReleasePointerCapture(this ElementReference element, int pointerId)
        => await GetJSRuntime(element).ElementReleasePointerCapture(element, pointerId);

    public static async ValueTask Remove(this ElementReference element)
        => await GetJSRuntime(element).ElementRemove(element);

    public static async ValueTask RemoveAttribute(this ElementReference element, string name)
        => await GetJSRuntime(element).ElementRemoveAttribute(element, name);

    public static async ValueTask RequestFullScreen(this ElementReference element, FullScreenOptions? options)
        => await GetJSRuntime(element).ElementRequestFullScreen(element, options);

    public static async ValueTask RequestPointerLock(this ElementReference element)
        => await GetJSRuntime(element).ElementRequestPointerLock(element);

    public static async ValueTask Scroll(this ElementReference element, ScrollToOptions? options)
        => await GetJSRuntime(element).ElementScroll(element, options, null, null);
    public static async ValueTask Scroll(this ElementReference element, double? x, double? y)
        => await GetJSRuntime(element).ElementScroll(element, null, x, y);

    public static async ValueTask ScrollBy(this ElementReference element, ScrollToOptions? options)
        => await GetJSRuntime(element).ElementScrollBy(element, options, null, null);
    public static async ValueTask ScrollBy(this ElementReference element, double? x, double? y)
        => await GetJSRuntime(element).ElementScrollBy(element, null, x, y);

    public static async ValueTask ScrollIntoView(this ElementReference element)
        => await GetJSRuntime(element).ElementScrollIntoView(element, null, null);
    public static async ValueTask ScrollIntoView(this ElementReference element, bool alignToTop)
        => await GetJSRuntime(element).ElementScrollIntoView(element, alignToTop, null);
    public static async ValueTask ScrollIntoView(this ElementReference element, ScrollIntoViewOptions options)
        => await GetJSRuntime(element).ElementScrollIntoView(element, null, options);

    public static async ValueTask SetAttribute(this ElementReference element, string name, string value)
        => await GetJSRuntime(element).ElementSetAttribute(element, name, value);

    public static async ValueTask SetPointerCapture(this ElementReference element, int pointerId)
        => await GetJSRuntime(element).ElementSetPointerCapture(element, pointerId);

    public static async ValueTask ToggleAttribute(this ElementReference element, string name, bool? force)
        => await GetJSRuntime(element).ElementToggleAttribute(element, name, force);

    public static async ValueTask<string> GetAccessKey(this ElementReference element)
        => await GetJSRuntime(element).ElementGetAccessKey(element);
    public static async ValueTask SetAccessKey(this ElementReference element, string value)
        => await GetJSRuntime(element).ElementSetAccessKey(element, value);

    public static async ValueTask<string> GetClassName(this ElementReference element)
        => await GetJSRuntime(element).ElementGetClassName(element);
    public static async ValueTask SetClassName(this ElementReference element, string value)
        => await GetJSRuntime(element).ElementSetClassName(element, value);

    public static async ValueTask<float> GetClientHeight(this ElementReference element)
        => await GetJSRuntime(element).ElementGetClientHeight(element);

    public static async ValueTask<float> GetClientLeft(this ElementReference element)
        => await GetJSRuntime(element).ElementGetClientLeft(element);

    public static async ValueTask<float> GetClientTop(this ElementReference element)
        => await GetJSRuntime(element).ElementGetClientTop(element);

    public static async ValueTask<float> GetClientWidth(this ElementReference element)
        => await GetJSRuntime(element).ElementGetClientWidth(element);

    public static async ValueTask<string> GetId(this ElementReference element)
        => await GetJSRuntime(element).ElementGetId(element);
    public static async ValueTask SetId(this ElementReference element, string value)
        => await GetJSRuntime(element).ElementSetId(element, value);

    public static async ValueTask<string> GetInnerHTML(this ElementReference element)
        => await GetJSRuntime(element).ElementGetInnerHTML(element);
    public static async ValueTask SetInnerHTML(this ElementReference element, string value)
        => await GetJSRuntime(element).ElementSetInnerHTML(element, value);

    public static async ValueTask<string> GetOuterHTML(this ElementReference element)
        => await GetJSRuntime(element).ElementGetOuterHTML(element);
    public static async ValueTask SetOuterHTML(this ElementReference element, string value)
        => await GetJSRuntime(element).ElementSetOuterHTML(element, value);

    public static async ValueTask<float> GetScrollHeight(this ElementReference element)
        => await GetJSRuntime(element).ElementGetScrollHeight(element);

    public static async ValueTask<float> GetScrollLeft(this ElementReference element)
        => await GetJSRuntime(element).ElementGetScrollLeft(element);

    public static async ValueTask<float> GetScrollTop(this ElementReference element)
        => await GetJSRuntime(element).ElementGetScrollTop(element);

    public static async ValueTask<float> GetScrollWidth(this ElementReference element)
        => await GetJSRuntime(element).ElementGetScrollWidth(element);

    public static async ValueTask<string> GetTagName(this ElementReference element)
        => await GetJSRuntime(element).ElementGetTagName(element);

    public static async ValueTask<ContentEditable> GetContentEditable(this ElementReference element)
        => await GetJSRuntime(element).ElementGetContentEditable(element);
    public static async ValueTask SetContentEditable(this ElementReference element, ContentEditable value)
        => await GetJSRuntime(element).ElementSetContentEditable(element, value);

    public static async ValueTask<bool> IsContentEditable(this ElementReference element)
        => await GetJSRuntime(element).ElementIsContentEditable(element);

    public static async ValueTask<ElementDir> GetDir(this ElementReference element)
        => await GetJSRuntime(element).ElementGetDir(element);
    public static async ValueTask SetDir(this ElementReference element, ElementDir value)
        => await GetJSRuntime(element).ElementSetDir(element, value);

    public static async ValueTask<EnterKeyHint> GetEnterKeyHint(this ElementReference element)
        => await GetJSRuntime(element).ElementGetEnterKeyHint(element);
    public static async ValueTask SetEnterKeyHint(this ElementReference element, EnterKeyHint value)
        => await GetJSRuntime(element).ElementSetEnterKeyHint(element, value);

    public static async ValueTask<Hidden> GetHidden(this ElementReference element)
        => await GetJSRuntime(element).ElementGetHidden(element);
    public static async ValueTask SetHidden(this ElementReference element, Hidden value)
        => await GetJSRuntime(element).ElementSetHidden(element, value);

    public static async ValueTask<bool> GetInert(this ElementReference element)
        => await GetJSRuntime(element).ElementGetInert(element);
    public static async ValueTask SetInert(this ElementReference element, bool value)
        => await GetJSRuntime(element).ElementSetInert(element, value);

    public static async ValueTask<string> GetInnerText(this ElementReference element)
        => await GetJSRuntime(element).ElementGetInnerText(element);
    public static async ValueTask SetInnerText(this ElementReference element, string value)
        => await GetJSRuntime(element).ElementSetInnerText(element, value);

    public static async ValueTask<InputMode> GetInputMode(this ElementReference element)
        => await GetJSRuntime(element).ElementGetInputMode(element);
    public static async ValueTask SetInputMode(this ElementReference element, InputMode value)
        => await GetJSRuntime(element).ElementSetInputMode(element, value);

    public static async ValueTask<float> GetOffsetHeight(this ElementReference element)
        => await GetJSRuntime(element).ElementGetOffsetHeight(element);

    public static async ValueTask<float> GetOffsetLeft(this ElementReference element)
        => await GetJSRuntime(element).ElementGetOffsetLeft(element);

    public static async ValueTask<float> GetOffsetTop(this ElementReference element)
        => await GetJSRuntime(element).ElementGetOffsetTop(element);

    public static async ValueTask<float> GetOffsetWidth(this ElementReference element)
        => await GetJSRuntime(element).ElementGetOffsetWidth(element);

    public static async ValueTask<int> GetTabIndex(this ElementReference element)
        => await GetJSRuntime(element).ElementGetTabIndex(element);
    public static async ValueTask SetTabIndex(this ElementReference element, int value)
        => await GetJSRuntime(element).ElementSetTabIndex(element, value);
}
