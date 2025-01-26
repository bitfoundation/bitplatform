using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Element is the most general base class from which all element objects (i.e. objects that represent elements) in a Document inherit. 
/// It only has methods and properties common to all kinds of elements. More specific classes inherit from Element.
/// The HTMLElement interface represents any HTML element. Some elements directly implement this interface, 
/// while others implement it via an interface that inherits it.
/// <br/>
/// More infor: <see href="https://developer.mozilla.org/en-US/docs/Web/API/element"></see>
/// More infor: <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement"></see>
/// </summary>
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

    /// <summary>
    /// Removes keyboard focus from the currently focused element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/blur">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/blur</see>
    /// </summary>
    public static ValueTask Blur(this ElementReference element)
        => GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.blur", element);

    /// <summary>
    /// Retrieves the value of the named attribute from the current node and returns it as a string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getAttribute">https://developer.mozilla.org/en-US/docs/Web/API/Element/getAttribute</see>
    /// </summary>
    public static ValueTask<string> GetAttribute(this ElementReference element, string name)
        => GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getAttribute", element, name);

    /// <summary>
    /// Returns an array of attribute names from the current element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getAttributeNames">https://developer.mozilla.org/en-US/docs/Web/API/Element/getAttributeNames</see>
    /// </summary>
    public static async ValueTask<string[]> GetAttributeNames(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string[]>("BitButil.element.getAttributeNames", element);

    /// <summary>
    /// Returns the size of an element and its position relative to the viewport.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect">https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect</see>
    /// </summary>
    public static async ValueTask<Rect> GetBoundingClientRect(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<Rect>("BitButil.element.getBoundingClientRect", element);

    /// <summary>
    /// Returns a boolean value indicating if the element has the specified attribute or not.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/hasAttribute">https://developer.mozilla.org/en-US/docs/Web/API/Element/hasAttribute</see>
    /// </summary>
    public static async ValueTask<bool> HasAttribute(this ElementReference element, string name)
        => await GetJSRuntime(element).FastInvokeAsync<bool>("BitButil.element.hasAttribute", element, name);

    /// <summary>
    /// Returns a boolean value indicating if the element has one or more HTML attributes present.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/hasAttributes">https://developer.mozilla.org/en-US/docs/Web/API/Element/hasAttributes</see>
    /// </summary>
    public static async ValueTask<bool> HasAttributes(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<bool>("BitButil.element.hasAttributes", element);

    /// <summary>
    /// Indicates whether the element on which it is invoked has pointer capture for the pointer identified by the given pointer ID.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/hasPointerCapture">https://developer.mozilla.org/en-US/docs/Web/API/Element/hasPointerCapture</see>
    /// </summary>
    public static async ValueTask<bool> HasPointerCapture(this ElementReference element, int pointerId)
        => await GetJSRuntime(element).FastInvokeAsync<bool>("BitButil.element.hasPointerCapture", element, pointerId);

    /// <summary>
    /// Returns a boolean value indicating whether or not the element would be selected by the specified selector string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/matches">https://developer.mozilla.org/en-US/docs/Web/API/Element/matches</see>
    /// </summary>
    public static async ValueTask<bool> Matches(this ElementReference element, string selectors)
        => await GetJSRuntime(element).FastInvokeAsync<bool>("BitButil.element.matches", element, selectors);

    /// <summary>
    /// Releases (stops) pointer capture that was previously set for a specific pointer event.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/releasePointerCapture">https://developer.mozilla.org/en-US/docs/Web/API/Element/releasePointerCapture</see>
    /// </summary>
    public static async ValueTask ReleasePointerCapture(this ElementReference element, int pointerId)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.releasePointerCapture", element, pointerId);

    /// <summary>
    /// Removes the element from the children list of its parent.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/remove">https://developer.mozilla.org/en-US/docs/Web/API/Element/remove</see>
    /// </summary>
    public static async ValueTask Remove(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.remove", element);

    /// <summary>
    /// Removes the named attribute from the current node.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/removeAttribute">https://developer.mozilla.org/en-US/docs/Web/API/Element/removeAttribute</see>
    /// </summary>
    public static async ValueTask RemoveAttribute(this ElementReference element, string name)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.removeAttribute", element, name);

    /// <summary>
    /// Asynchronously asks the browser to make the element fullscreen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/requestFullscreen">https://developer.mozilla.org/en-US/docs/Web/API/Element/requestFullscreen</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FullScreenJsOptions))]
    public static async ValueTask RequestFullScreen(this ElementReference element, FullScreenOptions? options)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.requestFullScreen", element, options?.ToJsObject());

    /// <summary>
    /// Allows to asynchronously ask for the pointer to be locked on the given element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/requestPointerLock">https://developer.mozilla.org/en-US/docs/Web/API/Element/requestPointerLock</see>
    /// </summary>
    public static async ValueTask RequestPointerLock(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.requestPointerLock", element);

    /// <summary>
    /// Scrolls to a particular set of coordinates inside a given element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scroll">https://developer.mozilla.org/en-US/docs/Web/API/Element/scroll</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScrollJsOptions))]
    public static async ValueTask Scroll(this ElementReference element, ScrollOptions? options)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.scroll", element, options?.ToJsObject(), null, null);
    /// <summary>
    /// Scrolls to a particular set of coordinates inside a given element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scroll">https://developer.mozilla.org/en-US/docs/Web/API/Element/scroll</see>
    /// </summary>
    public static async ValueTask Scroll(this ElementReference element, double? x, double? y)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.scroll", element, null, x, y);

    /// <summary>
    /// Scrolls an element by the given amount.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollBy">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollBy</see>
    /// </summary>
    public static async ValueTask ScrollBy(this ElementReference element, ScrollOptions? options)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.scrollBy", element, options?.ToJsObject(), null, null);
    /// <summary>
    /// Scrolls an element by the given amount.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollBy">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollBy</see>
    /// </summary>
    public static async ValueTask ScrollBy(this ElementReference element, double? x, double? y)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.scrollBy", element, null, x, y);

    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView</see>
    /// </summary>
    public static async ValueTask ScrollIntoView(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.scrollIntoView", element, null, null);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView</see>
    /// </summary>
    public static async ValueTask ScrollIntoView(this ElementReference element, bool alignToTop)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.scrollIntoView", element, alignToTop, null);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScrollIntoViewJsOptions))]
    public static async ValueTask ScrollIntoView(this ElementReference element, ScrollIntoViewOptions options)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.scrollIntoView", element, null, options?.ToJsObject());

    /// <summary>
    /// Sets the value of a named attribute of the current node.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/setAttribute">https://developer.mozilla.org/en-US/docs/Web/API/Element/setAttribute</see>
    /// </summary>
    public static async ValueTask SetAttribute(this ElementReference element, string name, string value)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setAttribute", element, name, value);

    /// <summary>
    /// Designates a specific element as the capture target of future pointer events.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/setPointerCapture">https://developer.mozilla.org/en-US/docs/Web/API/Element/setPointerCapture</see>
    /// </summary>
    public static async ValueTask SetPointerCapture(this ElementReference element, int pointerId)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setPointerCapture", element, pointerId);

    /// <summary>
    /// Toggles a boolean attribute, removing it if it is present and adding it if it is not present, on the specified element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/toggleAttribute">https://developer.mozilla.org/en-US/docs/Web/API/Element/toggleAttribute</see>
    /// </summary>
    public static async ValueTask<bool> ToggleAttribute(this ElementReference element, string name, bool? force)
        => await GetJSRuntime(element).FastInvokeAsync<bool>("BitButil.element.toggleAttribute", element, name, force);

    /// <summary>
    /// A string representing the access key assigned to the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/accessKey">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/accessKey</see>
    /// </summary>
    public static async ValueTask<string> GetAccessKey(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getAccessKey", element);
    /// <summary>
    /// A string representing the access key assigned to the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/accessKey">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/accessKey</see>
    /// </summary>
    public static async ValueTask SetAccessKey(this ElementReference element, string key)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setAccessKey", element, key);

    /// <summary>
    /// A string representing the class of the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/className">https://developer.mozilla.org/en-US/docs/Web/API/Element/className</see>
    /// </summary>
    public static async ValueTask<string> GetClassName(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getClassName", element);
    /// <summary>
    /// A string representing the class of the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/className">https://developer.mozilla.org/en-US/docs/Web/API/Element/className</see>
    /// </summary>
    public static async ValueTask SetClassName(this ElementReference element, string className)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setClassName", element, className);

    /// <summary>
    /// Returns a number representing the inner height of the element in px.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/clientHeight">https://developer.mozilla.org/en-US/docs/Web/API/Element/clientHeight</see>
    /// </summary>
    public static async ValueTask<float> GetClientHeight(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.clientHeight", element);

    /// <summary>
    /// Returns a number representing the width of the left border of the element in px.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/clientLeft">https://developer.mozilla.org/en-US/docs/Web/API/Element/clientLeft</see>
    /// </summary>
    public static async ValueTask<float> GetClientLeft(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.clientLeft", element);

    /// <summary>
    /// Returns a number representing the width of the top border of the element in px.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/clientTop">https://developer.mozilla.org/en-US/docs/Web/API/Element/clientTop</see>
    /// </summary>
    public static async ValueTask<float> GetClientTop(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.clientTop", element);

    /// <summary>
    /// Returns a number representing the inner width of the element in px.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/clientWidth">https://developer.mozilla.org/en-US/docs/Web/API/Element/clientWidth</see>
    /// </summary>
    public static async ValueTask<float> GetClientWidth(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.clientWidth", element);

    /// <summary>
    /// A string representing the id of the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/id">https://developer.mozilla.org/en-US/docs/Web/API/Element/id</see>
    /// </summary>
    public static async ValueTask<string> GetId(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getId", element);
    /// <summary>
    /// A string representing the id of the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/id">https://developer.mozilla.org/en-US/docs/Web/API/Element/id</see>
    /// </summary>
    public static async ValueTask SetId(this ElementReference element, string id)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setId", element, id);

    /// <summary>
    /// A string representing the markup of the element's content.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/innerHTML">https://developer.mozilla.org/en-US/docs/Web/API/Element/innerHTML</see>
    /// </summary>
    public static async ValueTask<string> GetInnerHtml(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getInnerHTML", element);
    /// <summary>
    /// A string representing the markup of the element's content.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/innerHTML">https://developer.mozilla.org/en-US/docs/Web/API/Element/innerHTML</see>
    /// </summary>
    public static async ValueTask SetInnerHtml(this ElementReference element, string innerHtml)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setInnerHTML", element, innerHtml);

    /// <summary>
    /// A string representing the markup of the element including its content.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/outerHTML">https://developer.mozilla.org/en-US/docs/Web/API/Element/outerHTML</see>
    /// </summary>
    public static async ValueTask<string> GetOuterHtml(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getOuterHTML", element);
    /// <summary>
    /// A string representing the markup of the element including its content. When used as a setter, 
    /// replaces the element with nodes parsed from the given string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/outerHTML">https://developer.mozilla.org/en-US/docs/Web/API/Element/outerHTML</see>
    /// </summary>
    public static async ValueTask SetOuterHtml(this ElementReference element, string outerHtml)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setOuterHTML", element, outerHtml);

    /// <summary>
    /// Returns a number representing the scroll view height of an element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollHeight">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollHeight</see>
    /// </summary>
    public static async ValueTask<float> GetScrollHeight(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.scrollHeight", element);

    /// <summary>
    /// A number representing the left scroll offset of the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollLeft">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollLeft</see>
    /// </summary>
    public static async ValueTask<float> GetScrollLeft(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.scrollLeft", element);

    /// <summary>
    /// A number representing number of pixels the top of the element is scrolled vertically.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollTop">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollTop</see>
    /// </summary>
    public static async ValueTask<float> GetScrollTop(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.scrollTop", element);

    /// <summary>
    /// Returns a number representing the scroll view width of the element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollWidth">https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollWidth</see>
    /// </summary>
    public static async ValueTask<float> GetScrollWidth(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.scrollWidth", element);

    /// <summary>
    /// Returns a string with the name of the tag for the given element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/tagName">https://developer.mozilla.org/en-US/docs/Web/API/Element/tagName</see>
    /// </summary>
    public static async ValueTask<string> GetTagName(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.tagName", element);

    /// <summary>
    /// The contentEditable property of the HTMLElement interface specifies whether or not the element is editable.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/contentEditable">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/contentEditable</see>
    /// </summary>
    public static async ValueTask<ContentEditable> GetContentEditable(this ElementReference element)
    {
        var value = await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getContentEditable", element);
        return value switch
        {
            "true" => ContentEditable.True,
            "false" => ContentEditable.False,
            "plaintext-only" => ContentEditable.PlainTextOnly,
            _ => ContentEditable.Inherit
        };
    }
    /// <summary>
    /// The contentEditable property of the HTMLElement interface specifies whether or not the element is editable.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/contentEditable">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/contentEditable</see>
    /// </summary>
    public static async ValueTask SetContentEditable(this ElementReference element, ContentEditable value)
    {
        var v = value switch
        {
            ContentEditable.False => "false",
            ContentEditable.True => "true",
            ContentEditable.PlainTextOnly => "plaintext-only",
            _ => "inherit",
        };
        await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setContentEditable", element, v);
    }

    /// <summary>
    /// Returns a boolean value indicating whether or not the content of the element can be edited.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/isContentEditable">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/isContentEditable</see>
    /// </summary>
    public static async ValueTask<bool> IsContentEditable(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<bool>("BitButil.element.isContentEditable", element);

    /// <summary>
    /// The HTMLElement.dir property gets or sets the text writing directionality of the content of the current element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dir">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dir</see>
    /// </summary>
    public static async ValueTask<ElementDir> GetDir(this ElementReference element)
    {
        var value = await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getDir", element);
        return value switch
        {
            "ltr" => ElementDir.Ltr,
            "rtl" => ElementDir.Rtl,
            "auto" => ElementDir.Auto,
            _ => ElementDir.NotSet,
        };
    }
    /// <summary>
    /// The HTMLElement.dir property gets or sets the text writing directionality of the content of the current element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dir">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dir</see>
    /// </summary>
    public static async ValueTask SetDir(this ElementReference element, ElementDir value)
    {
        var v = value switch
        {
            ElementDir.Ltr => "ltr",
            ElementDir.Rtl => "rtl",
            ElementDir.Auto => "auto",
            _ => "",
        };
        await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setDir", element, v);
    }

    /// <summary>
    /// The enterKeyHint property is an enumerated property defining what action label (or icon) 
    /// to present for the enter key on virtual keyboards.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/enterKeyHint">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/enterKeyHint</see>
    /// </summary>
    public static async ValueTask<EnterKeyHint> GetEnterKeyHint(this ElementReference element)
    {
        var value = await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getEnterKeyHint", element);
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
    /// <summary>
    /// The enterKeyHint property is an enumerated property defining what action label (or icon) 
    /// to present for the enter key on virtual keyboards.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/enterKeyHint">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/enterKeyHint</see>
    /// </summary>
    public static async ValueTask SetEnterKeyHint(this ElementReference element, EnterKeyHint value)
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
        await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setEnterKeyHint", element, v);
    }

    /// <summary>
    /// The HTMLElement property hidden reflects the value of the element's hidden attribute.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/hidden">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/hidden</see>
    /// </summary>
    public static async ValueTask<Hidden> GetHidden(this ElementReference element)
    {
        var value = await GetJSRuntime(element).FastInvokeAsync<object>("BitButil.element.getHidden", element);
        var v = value.ToString() switch
        {
            "True" => Hidden.True,
            "until-found" => Hidden.UntilFound,
            _ => Hidden.False
        };
        return v;
    }
    /// <summary>
    /// The HTMLElement property hidden reflects the value of the element's hidden attribute.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/hidden">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/hidden</see>
    /// </summary>
    public static async ValueTask SetHidden(this ElementReference element, Hidden value)
    {
        object v = value switch
        {
            Hidden.True => true,
            Hidden.UntilFound => "until-found",
            _ => false,
        };
        await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setHidden", element, v);
    }

    /// <summary>
    /// The HTMLElement property inert reflects the value of the element's inert attribute. 
    /// It is a boolean value that, when present, makes the browser "ignore" user input events 
    /// for the element, including focus events and events from assistive technologies.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inert">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inert</see>
    /// </summary>
    public static async ValueTask<bool> GetInert(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<bool>("BitButil.element.getInert", element);
    /// <summary>
    /// The HTMLElement property inert reflects the value of the element's inert attribute. 
    /// It is a boolean value that, when present, makes the browser "ignore" user input events 
    /// for the element, including focus events and events from assistive technologies.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inert">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inert</see>
    /// </summary>
    public static async ValueTask SetInert(this ElementReference element, bool value)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setInert", element, value);

    /// <summary>
    /// The innerText property of the HTMLElement interface represents the rendered text content of a node and its descendants.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/innerText">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/innerText</see>
    /// </summary>
    public static async ValueTask<string> GetInnerText(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getInnerText", element);
    /// <summary>
    /// The innerText property of the HTMLElement interface represents the rendered text content of a node and its descendants.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/innerText">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/innerText</see>
    /// </summary>
    public static async ValueTask SetInnerText(this ElementReference element, string value)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setInnerText", element, value);

    /// <summary>
    /// The HTMLElement property inputMode reflects the value of the element's inputmode attribute.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inputMode">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inputMode</see>
    /// </summary>
    public static async ValueTask<InputMode> GetInputMode(this ElementReference element)
    {
        var value = await GetJSRuntime(element).FastInvokeAsync<string>("BitButil.element.getInputMode", element);
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
    /// <summary>
    /// The HTMLElement property inputMode reflects the value of the element's inputmode attribute.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inputMode">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/inputMode</see>
    /// </summary>
    public static async ValueTask SetInputMode(this ElementReference element, InputMode value)
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
        await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setInputMode", element, v);
    }

    /// <summary>
    /// The HTMLElement.offsetHeight read-only property returns the height of an element, 
    /// including vertical padding and borders in px.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetHeight">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetHeight</see>
    /// </summary>
    public static async ValueTask<float> GetOffsetHeight(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.offsetHeight", element);

    /// <summary>
    /// The HTMLElement.offsetLeft read-only property returns the number of pixels that the upper left corner 
    /// of the current element is offset to the left within the HTMLElement.offsetParent node.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetLeft">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetLeft</see>
    /// </summary>
    public static async ValueTask<float> GetOffsetLeft(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.offsetLeft", element);

    /// <summary>
    /// The HTMLElement.offsetTop read-only property returns the distance from the outer border of the current element 
    /// (including its margin) to the top padding edge of the offsetParent, the closest positioned ancestor element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetTop">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetTop</see>
    /// </summary>
    public static async ValueTask<float> GetOffsetTop(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.offsetTop", element);

    /// <summary>
    /// The layout width of an element in px.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetWidth">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetWidth</see>
    /// </summary>
    public static async ValueTask<float> GetOffsetWidth(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<float>("BitButil.element.offsetWidth", element);

    /// <summary>
    /// A number representing the position of the element in the tabbing order.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/tabIndex">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/tabIndex</see>
    /// </summary>
    public static async ValueTask<int> GetTabIndex(this ElementReference element)
        => await GetJSRuntime(element).FastInvokeAsync<int>("BitButil.element.getTabIndex", element);
    /// <summary>
    /// A number representing the position of the element in the tabbing order.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/tabIndex">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/tabIndex</see>
    /// </summary>
    public static async ValueTask SetTabIndex(this ElementReference element, int value)
        => await GetJSRuntime(element).FastInvokeVoidAsync("BitButil.element.setTabIndex", element, value);
}
