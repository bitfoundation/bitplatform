using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class ElementJsInterop
{
    internal static async ValueTask ElementBlur(this IJSRuntime js, ElementReference element)
        => await js.InvokeVoidAsync("BitButil.element.blur", element);

    internal static async ValueTask<string> ElementGetAttribute(this IJSRuntime js, ElementReference element, string name)
        => await js.InvokeAsync<string>("BitButil.element.getAttribute", element, name);

    internal static async ValueTask<string[]> ElementGetAttributeNames(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string[]>("BitButil.element.getAttributeNames", element);

    internal static async ValueTask<Rect> ElementGetBoundingClientRect(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<Rect>("BitButil.element.getBoundingClientRect", element);

    internal static async ValueTask<bool> ElementHasAttribute(this IJSRuntime js, ElementReference element, string name)
        => await js.InvokeAsync<bool>("BitButil.element.hasAttribute", element, name);

    internal static async ValueTask<bool> ElementHasAttributes(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<bool>("BitButil.element.hasAttributes", element);

    internal static async ValueTask<bool> ElementHasPointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
        => await js.InvokeAsync<bool>("BitButil.element.hasPointerCapture", element, pointerId);

    internal static async ValueTask<bool> ElementMatches(this IJSRuntime js, ElementReference element, string selectors)
        => await js.InvokeAsync<bool>("BitButil.element.matches", element, selectors);

    internal static async ValueTask ElementReleasePointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
        => await js.InvokeVoidAsync("BitButil.element.releasePointerCapture", element, pointerId);

    internal static async ValueTask ElementRemove(this IJSRuntime js, ElementReference element)
        => await js.InvokeVoidAsync("BitButil.element.remove", element);

    internal static async ValueTask<string> ElementRemoveAttribute(this IJSRuntime js, ElementReference element, string name)
        => await js.InvokeAsync<string>("BitButil.element.removeAttribute", element, name);

    internal static async ValueTask ElementRequestFullScreen(this IJSRuntime js, ElementReference element, FullScreenOptions? options)
        => await js.InvokeVoidAsync("BitButil.element.requestFullScreen", element, options);

    internal static async ValueTask ElementRequestPointerLock(this IJSRuntime js, ElementReference element)
        => await js.InvokeVoidAsync("BitButil.element.requestPointerLock", element);

    internal static async ValueTask ElementScroll(this IJSRuntime js, ElementReference element, ScrollToOptions? options, double? x, double? y)
        => await js.InvokeVoidAsync("BitButil.element.scroll", element, options, x, y);

    internal static async ValueTask ElementScrollBy(this IJSRuntime js, ElementReference element, ScrollToOptions? options, double? x, double? y)
        => await js.InvokeVoidAsync("BitButil.element.scrollBy", element, options?.ToJsObject(), x, y);

    internal static async ValueTask ElementScrollIntoView(this IJSRuntime js, ElementReference element, bool? alignToTop, ScrollIntoViewOptions? options)
        => await js.InvokeVoidAsync("BitButil.element.scrollIntoView", element, alignToTop, options);

    internal static async ValueTask ElementSetAttribute(this IJSRuntime js, ElementReference element, string name, string value)
        => await js.InvokeVoidAsync("BitButil.element.setAttribute", element, name, value);

    internal static async ValueTask ElementSetPointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
        => await js.InvokeVoidAsync("BitButil.element.setPointerCapture", element, pointerId);

    internal static async ValueTask ElementToggleAttribute(this IJSRuntime js, ElementReference element, string name, bool? force)
        => await js.InvokeVoidAsync("BitButil.element.toggleAttribute", element, name, force);

    internal static async ValueTask<string> ElementGetAccessKey(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getAccessKey", element);
    internal static async ValueTask ElementSetAccessKey(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setAccessKey", element, value);

    internal static async ValueTask<string> ElementGetClassName(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getClassName", element);
    internal static async ValueTask ElementSetClassName(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setClassName", element, value);

    internal static async ValueTask<float> ElementGetClientHeight(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientHeight", element);

    internal static async ValueTask<float> ElementGetClientLeft(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientLeft", element);

    internal static async ValueTask<float> ElementGetClientTop(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientTop", element);

    internal static async ValueTask<float> ElementGetClientWidth(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.clientWidth", element);

    internal static async ValueTask<string> ElementGetId(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getId", element);
    internal static async ValueTask ElementSetId(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setId", element, value);

    internal static async ValueTask<string> ElementGetInnerHTML(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getInnerHTML", element);
    internal static async ValueTask ElementSetInnerHTML(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setInnerHTML", element, value);

    internal static async ValueTask<string> ElementGetOuterHTML(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getOuterHTML", element);
    internal static async ValueTask ElementSetOuterHTML(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setOuterHTML", element, value);

    internal static async ValueTask<float> ElementGetScrollHeight(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollHeight", element);

    internal static async ValueTask<float> ElementGetScrollLeft(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollLeft", element);

    internal static async ValueTask<float> ElementGetScrollTop(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollTop", element);

    internal static async ValueTask<float> ElementGetScrollWidth(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollWidth", element);

    internal static async ValueTask<string> ElementGetTagName(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.tagName", element);

    internal static async ValueTask<ContentEditable> ElementGetContentEditable(this IJSRuntime js, ElementReference element)
    {
        var value = await js.InvokeAsync<string>("BitButil.element.getContentEditable", element);
        return value switch
        {
            "true" => ContentEditable.True,
            "false" => ContentEditable.False,
            "plaintext-only" => ContentEditable.PlainTextOnly,
            _ => ContentEditable.Inherit
        };
    }
    internal static async ValueTask ElementSetContentEditable(this IJSRuntime js, ElementReference element, ContentEditable value)
    {
        var v = value switch
        {
            ContentEditable.False => "false",
            ContentEditable.True => "true",
            ContentEditable.PlainTextOnly => "plaintext-only",
            _ => "inherit",
        };
        await js.InvokeVoidAsync("BitButil.element.setContentEditable", element, v);
    }

    internal static async ValueTask<bool> ElementIsContentEditable(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<bool>("BitButil.element.isContentEditable", element);

    internal static async ValueTask<ElementDir> ElementGetDir(this IJSRuntime js, ElementReference element)
    {
        var value = await js.InvokeAsync<string>("BitButil.element.getDir", element);
        return value switch
        {
            "ltr" => ElementDir.Ltr,
            "rtl" => ElementDir.Rtl,
            "auto" => ElementDir.Auto,
            _ => ElementDir.NotSet,
        };
    }
    internal static async ValueTask ElementSetDir(this IJSRuntime js, ElementReference element, ElementDir value)
    {
        var v = value switch
        {
            ElementDir.Ltr => "ltr",
            ElementDir.Rtl => "rtl",
            ElementDir.Auto => "auto",
            _ => "",
        };
        await js.InvokeVoidAsync("BitButil.element.setDir", element, v);
    }

    internal static async ValueTask<EnterKeyHint> ElementGetEnterKeyHint(this IJSRuntime js, ElementReference element)
    {
        var value = await js.InvokeAsync<string>("BitButil.element.getEnterKeyHint", element);
        return value switch
        {
            "enter" => EnterKeyHint.Enter,
            "done" => EnterKeyHint.Done,
            "go" => EnterKeyHint.Go,
            "next" => EnterKeyHint.Next,
            "previous" => EnterKeyHint.Previous,
            "search" => EnterKeyHint.Search,
            "send" => EnterKeyHint.Send,
            _ => EnterKeyHint.NotSet
        };
    }
    internal static async ValueTask ElementSetEnterKeyHint(this IJSRuntime js, ElementReference element, EnterKeyHint value)
    {
        var v = value switch
        {
            EnterKeyHint.Enter => "enter",
            EnterKeyHint.Done => "done",
            EnterKeyHint.Go => "go",
            EnterKeyHint.Next => "next",
            EnterKeyHint.Previous => "previous",
            EnterKeyHint.Search => "search",
            EnterKeyHint.Send => "send",
            _ => "",
        };
        await js.InvokeVoidAsync("BitButil.element.setEnterKeyHint", element, v);
    }

    internal static async ValueTask<Hidden> ElementGetHidden(this IJSRuntime js, ElementReference element)
    {
        var value = await js.InvokeAsync<string>("BitButil.element.getHidden", element);
        return value switch
        {
            "true" => Hidden.True,
            "until-found" => Hidden.UntilFound,
            _ => Hidden.False
        };
    }
    internal static async ValueTask ElementSetHidden(this IJSRuntime js, ElementReference element, Hidden value)
    {
        var v = value switch
        {
            Hidden.True => "true",
            Hidden.UntilFound => "until-found",
            _ => "false",
        };
        await js.InvokeVoidAsync("BitButil.element.setHidden", element, v);
    }

    internal static async ValueTask<bool> ElementGetInert(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<bool>("BitButil.element.getInert", element);
    internal static async ValueTask ElementSetInert(this IJSRuntime js, ElementReference element, bool value)
        => await js.InvokeVoidAsync("BitButil.element.setInert", element, value);

    internal static async ValueTask<string> ElementGetInnerText(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getInnerText", element);
    internal static async ValueTask ElementSetInnerText(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setInnerText", element, value);

    internal static async ValueTask<InputMode> ElementGetInputMode(this IJSRuntime js, ElementReference element)
    {
        var value = await js.InvokeAsync<string>("BitButil.element.getInputMode", element);
        return value switch
        {
            "decimal" => InputMode.Decimal,
            "email" => InputMode.Email,
            "none" => InputMode.None,
            "numeric" => InputMode.Numeric,
            "search" => InputMode.Search,
            "tel" => InputMode.Tel,
            "text" => InputMode.Text,
            "url" => InputMode.Url,
            _ => InputMode.NotSet,
        };
    }
    internal static async ValueTask ElementSetInputMode(this IJSRuntime js, ElementReference element, InputMode value)
    {
        var v = value switch
        {
            InputMode.Decimal => "decimal",
            InputMode.Email => "email",
            InputMode.None => "none",
            InputMode.Numeric => "numeric",
            InputMode.Search => "search",
            InputMode.Tel => "tel",
            InputMode.Text => "text",
            InputMode.Url => "url",
            _ => "",
        };
        await js.InvokeVoidAsync("BitButil.element.setInputMode", element, v);
    }

    internal static async ValueTask<float> ElementGetOffsetHeight(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.offsetHeight", element);

    internal static async ValueTask<float> ElementGetOffsetLeft(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.offsetLeft", element);

    internal static async ValueTask<float> ElementGetOffsetTop(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.offsetTop", element);

    internal static async ValueTask<float> ElementGetOffsetWidth(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.offsetWidth", element);

    internal static async ValueTask<int> ElementGetTabIndex(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<int>("BitButil.element.getTabIndex", element);
    internal static async ValueTask ElementSetTabIndex(this IJSRuntime js, ElementReference element, int value)
        => await js.InvokeVoidAsync("BitButil.element.setTabIndex", element, value);
}
