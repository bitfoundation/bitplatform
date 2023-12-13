﻿using System.Threading.Tasks;
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

    internal static async Task<bool> ElementHasAttribute(this IJSRuntime js, ElementReference element, string name)
        => await js.InvokeAsync<bool>("BitButil.element.hasAttribute", element, name);

    internal static async Task<bool> ElementHasAttributes(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<bool>("BitButil.element.hasAttributes", element);

    internal static async Task<bool> ElementHasPointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
        => await js.InvokeAsync<bool>("BitButil.element.hasPointerCapture", element, pointerId);

    internal static async Task<bool> ElementMatches(this IJSRuntime js, ElementReference element, string selectors)
        => await js.InvokeAsync<bool>("BitButil.element.matches", element, selectors);

    internal static async Task ElementReleasePointerCapture(this IJSRuntime js, ElementReference element, int pointerId)
        => await js.InvokeVoidAsync("BitButil.element.releasePointerCapture", element, pointerId);

    internal static async Task ElementRemove(this IJSRuntime js, ElementReference element)
        => await js.InvokeVoidAsync("BitButil.element.remove", element);

    internal static async Task<string> ElementRemoveAttribute(this IJSRuntime js, ElementReference element, string name)
        => await js.InvokeAsync<string>("BitButil.element.removeAttribute", element, name);

    internal static async Task ElementRequestFullScreen(this IJSRuntime js, ElementReference element, FullScreenOptions? options)
        => await js.InvokeVoidAsync("BitButil.element.requestFullScreen", element, options);

    internal static async Task ElementRequestPointerLock(this IJSRuntime js, ElementReference element)
        => await js.InvokeVoidAsync("BitButil.element.requestPointerLock", element);

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

    internal static async Task ElementToggleAttribute(this IJSRuntime js, ElementReference element, string name, bool? force)
        => await js.InvokeVoidAsync("BitButil.element.toggleAttribute", element, name, force);

    internal static async Task<string> ElementGetAccessKey(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getAccessKey", element);
    internal static async Task ElementSetAccessKey(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setAccessKey", element, value);

    internal static async Task<string> ElementGetClassName(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getClassName", element);
    internal static async Task ElementSetClassName(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setClassName", element, value);

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
    internal static async Task ElementSetId(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setId", element, value);

    internal static async Task<string> ElementGetInnerHTML(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getInnerHTML", element);
    internal static async Task ElementSetInnerHTML(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setInnerHTML", element, value);

    internal static async Task<string> ElementGetOuterHTML(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getOuterHTML", element);
    internal static async Task ElementSetOuterHTML(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setOuterHTML", element, value);

    internal static async Task<float> ElementGetScrollHeight(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollHeight", element);

    internal static async Task<float> ElementGetScrollLeft(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollLeft", element);

    internal static async Task<float> ElementGetScrollTop(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollTop", element);

    internal static async Task<float> ElementGetScrollWidth(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<float>("BitButil.element.scrollWidth", element);

    internal static async Task<string> ElementGetTagName(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.tagName", element);

    internal static async Task<ContentEditable> ElementGetContentEditable(this IJSRuntime js, ElementReference element)
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
    internal static async Task ElementSetContentEditable(this IJSRuntime js, ElementReference element, ContentEditable value)
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

    internal static async Task<bool> ElementIsContentEditable(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<bool>("BitButil.element.isContentEditable", element);

    internal static async Task<Dir> ElementGetDir(this IJSRuntime js, ElementReference element)
    {
        var value = await js.InvokeAsync<string>("BitButil.element.getDir", element);
        return value switch
        {
            "ltr" => Dir.Ltr,
            "rtl" => Dir.Rtl,
            "auto" => Dir.Auto,
            _ => Dir.NotSet,
        };
    }
    internal static async Task ElementSetDir(this IJSRuntime js, ElementReference element, Dir value)
    {
        var v = value switch
        {
            Dir.Ltr => "ltr",
            Dir.Rtl => "rtl",
            Dir.Auto => "auto",
            _ => "",
        };
        await js.InvokeVoidAsync("BitButil.element.setDir", element, v);
    }

    internal static async Task<EnterKeyHint> ElementGetEnterKeyHint(this IJSRuntime js, ElementReference element)
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
    internal static async Task ElementSetEnterKeyHint(this IJSRuntime js, ElementReference element, EnterKeyHint value)
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

    internal static async Task<Hidden> ElementGetHidden(this IJSRuntime js, ElementReference element)
    {
        var value = await js.InvokeAsync<string>("BitButil.element.getHidden", element);
        return value switch
        {
            "true" => Hidden.True,
            "until-found" => Hidden.UntilFound,
            _ => Hidden.False
        };
    }
    internal static async Task ElementSetHidden(this IJSRuntime js, ElementReference element, Hidden value)
    {
        var v = value switch
        {
            Hidden.True => "true",
            Hidden.UntilFound => "until-found",
            _ => "false",
        };
        await js.InvokeVoidAsync("BitButil.element.setHidden", element, v);
    }

    internal static async Task<bool> ElementGetInert(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<bool>("BitButil.element.getInert", element);
    internal static async Task ElementSetInert(this IJSRuntime js, ElementReference element, bool value)
        => await js.InvokeVoidAsync("BitButil.element.setInert", element, value);

    internal static async Task<string> ElementGetInnerText(this IJSRuntime js, ElementReference element)
        => await js.InvokeAsync<string>("BitButil.element.getInnerText", element);
    internal static async Task ElementSetInnerText(this IJSRuntime js, ElementReference element, string value)
        => await js.InvokeVoidAsync("BitButil.element.setInnerText", element, value);

    internal static async Task<InputMode> ElementGetInputMode(this IJSRuntime js, ElementReference element)
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
    internal static async Task ElementSetInputMode(this IJSRuntime js, ElementReference element, InputMode value)
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
}
