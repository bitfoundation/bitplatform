using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

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
public class Element(IJSRuntime js)
{
    /// <summary>
    /// Removes keyboard focus from the currently focused element.
    /// </summary>
    public async Task Blur(ElementReference element)
        => await js.ElementBlur(element);

    /// <summary>
    /// Retrieves the value of the named attribute from the current node and returns it as a string.
    /// </summary>
    public async Task<string> GetAttribute(ElementReference element, string name)
        => await js.ElementGetAttribute(element, name);

    /// <summary>
    /// Returns an array of attribute names from the current element.
    /// </summary>
    public async Task<string[]> GetAttributeNames(ElementReference element)
        => await js.ElementGetAttributeNames(element);

    /// <summary>
    /// Returns the size of an element and its position relative to the viewport.
    /// </summary>
    public async Task<Rect> GetBoundingClientRect(ElementReference element)
        => await js.ElementGetBoundingClientRect(element);

    /// <summary>
    /// Returns a boolean value indicating if the element has the specified attribute or not.
    /// </summary>
    public async Task<bool> HasAttribute(ElementReference element, string name)
        => await js.ElementHasAttribute(element, name);

    /// <summary>
    /// Returns a boolean value indicating if the element has one or more HTML attributes present.
    /// </summary>
    public async Task<bool> HasAttributes(ElementReference element)
        => await js.ElementHasAttributes(element);

    /// <summary>
    /// Indicates whether the element on which it is invoked has pointer capture for the pointer identified by the given pointer ID.
    /// </summary>
    public async Task<bool> HasPointerCapture(ElementReference element, int pointerId)
        => await js.ElementHasPointerCapture(element, pointerId);

    /// <summary>
    /// Returns a boolean value indicating whether or not the element would be selected by the specified selector string.
    /// </summary>
    public async Task<bool> Matches(ElementReference element, string selectors)
        => await js.ElementMatches(element, selectors);

    /// <summary>
    /// Releases (stops) pointer capture that was previously set for a specific pointer event.
    /// </summary>
    public async Task ReleasePointerCapture(ElementReference element, int pointerId)
        => await js.ElementReleasePointerCapture(element, pointerId);

    /// <summary>
    /// Removes the element from the children list of its parent.
    /// </summary>
    public async Task Remove(ElementReference element)
        => await js.ElementRemove(element);

    /// <summary>
    /// Removes the named attribute from the current node.
    /// </summary>
    public async Task RemoveAttribute(ElementReference element, string name)
        => await js.ElementRemoveAttribute(element, name);

    /// <summary>
    /// Asynchronously asks the browser to make the element fullscreen.
    /// </summary>
    public async Task RequestFullScreen(ElementReference element, FullScreenOptions? options)
        => await js.ElementRequestFullScreen(element, options);

    /// <summary>
    /// Allows to asynchronously ask for the pointer to be locked on the given element.
    /// </summary>
    public async Task RequestPointerLock(ElementReference element)
        => await js.ElementRequestPointerLock(element);

    /// <summary>
    /// Scrolls to a particular set of coordinates inside a given element.
    /// </summary>
    public async Task Scroll(ElementReference element, ScrollToOptions? options)
        => await js.ElementScroll(element, options, null, null);
    /// <summary>
    /// Scrolls to a particular set of coordinates inside a given element.
    /// </summary>
    public async Task Scroll(ElementReference element, double? x, double? y)
        => await js.ElementScroll(element, null, x, y);

    /// <summary>
    /// Scrolls an element by the given amount.
    /// </summary>
    public async Task ScrollBy(ElementReference element, ScrollToOptions? options)
        => await js.ElementScrollBy(element, options, null, null);
    /// <summary>
    /// Scrolls an element by the given amount.
    /// </summary>
    public async Task ScrollBy(ElementReference element, double? x, double? y)
        => await js.ElementScrollBy(element, null, x, y);

    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element)
        => await ScrollIntoView(element, null, null);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element, bool alignToTop)
        => await ScrollIntoView(element, alignToTop, null);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element, ScrollIntoViewOptions options)
        => await ScrollIntoView(element, null, options);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element, bool? alignToTop, ScrollIntoViewOptions? options)
        => await js.ElementScrollIntoView(element, alignToTop, options);

    /// <summary>
    /// Sets the value of a named attribute of the current node.
    /// </summary>
    public async Task SetAttribute(ElementReference element, string name, string value)
        => await js.ElementSetAttribute(element, name, value);

    /// <summary>
    /// Designates a specific element as the capture target of future pointer events.
    /// </summary>
    public async Task SetPointerCapture(ElementReference element, int pointerId)
        => await js.ElementSetPointerCapture(element, pointerId);

    /// <summary>
    /// Toggles a boolean attribute, removing it if it is present and adding it if it is not present, on the specified element.
    /// </summary>
    public async Task ToggleAttribute(ElementReference element, string name, bool? force = null)
        => await js.ElementToggleAttribute(element, name, force);

    /// <summary>
    /// A string representing the access key assigned to the element.
    /// </summary>
    public async Task<string> GetAccessKey(ElementReference element)
        => await js.ElementGetAccessKey(element);
    /// <summary>
    /// A string representing the access key assigned to the element.
    /// </summary>
    public async Task SetAccessKey(ElementReference element, string key)
        => await js.ElementSetAccessKey(element, key);

    /// <summary>
    /// A string representing the class of the element.
    /// </summary>
    public async Task<string> GetClassName(ElementReference element)
        => await js.ElementGetClassName(element);
    /// <summary>
    /// A string representing the class of the element.
    /// </summary>
    public async Task SetClassName(ElementReference element, string className)
        => await js.ElementSetClassName(element, className);

    /// <summary>
    /// Returns a number representing the inner height of the element in px.
    /// </summary>
    public async Task<float> GetClientHeight(ElementReference element)
        => await js.ElementGetClientHeight(element);

    /// <summary>
    /// Returns a number representing the width of the left border of the element in px.
    /// </summary>
    public async Task<float> GetClientLeft(ElementReference element)
        => await js.ElementGetClientLeft(element);

    /// <summary>
    /// Returns a number representing the width of the top border of the element in px.
    /// </summary>
    public async Task<float> GetClientTop(ElementReference element)
        => await js.ElementGetClientTop(element);

    /// <summary>
    /// Returns a number representing the inner width of the element in px.
    /// </summary>
    public async Task<float> GetClientWidth(ElementReference element)
        => await js.ElementGetClientWidth(element);

    /// <summary>
    /// A string representing the id of the element.
    /// </summary>
    public async Task<string> GetId(ElementReference element)
        => await js.ElementGetId(element);
    /// <summary>
    /// A string representing the id of the element.
    /// </summary>
    public async Task SetId(ElementReference element, string id)
        => await js.ElementSetId(element, id);

    /// <summary>
    /// A string representing the markup of the element's content.
    /// </summary>
    public async Task<string> GetInnerHtml(ElementReference element)
        => await js.ElementGetInnerHTML(element);
    /// <summary>
    /// A string representing the markup of the element's content.
    /// </summary>
    public async Task SetInnerHtml(ElementReference element, string innerHtml)
        => await js.ElementSetInnerHTML(element, innerHtml);

    /// <summary>
    /// A string representing the markup of the element including its content.
    /// </summary>
    public async Task<string> GetOuterHtml(ElementReference element)
        => await js.ElementGetOuterHTML(element);
    /// <summary>
    /// A string representing the markup of the element including its content. When used as a setter, replaces the element with nodes parsed from the given string.
    /// </summary>
    public async Task SetOuterHtml(ElementReference element, string outerHtml)
        => await js.ElementSetOuterHTML(element, outerHtml);

    /// <summary>
    /// Returns a number representing the scroll view height of an element.
    /// </summary>
    public async Task<float> GetScrollHeight(ElementReference element)
        => await js.ElementGetScrollHeight(element);

    /// <summary>
    /// A number representing the left scroll offset of the element.
    /// </summary>
    public async Task<float> GetScrollLeft(ElementReference element)
        => await js.ElementGetScrollLeft(element);

    /// <summary>
    /// A number representing number of pixels the top of the element is scrolled vertically.
    /// </summary>
    public async Task<float> GetScrollTop(ElementReference element)
        => await js.ElementGetScrollTop(element);

    /// <summary>
    /// Returns a number representing the scroll view width of the element.
    /// </summary>
    public async Task<float> GetScrollWidth(ElementReference element)
        => await js.ElementGetScrollWidth(element);

    /// <summary>
    /// Returns a string with the name of the tag for the given element.
    /// </summary>
    public async Task<float> GetTagName(ElementReference element)
        => await js.ElementGetScrollWidth(element);

    /// <summary>
    /// The contentEditable property of the HTMLElement interface specifies whether or not the element is editable.
    /// </summary>
    public async Task<ContentEditable> GetContentEditable(ElementReference element)
        => await js.ElementGetContentEditable(element);
    /// <summary>
    /// The contentEditable property of the HTMLElement interface specifies whether or not the element is editable.
    /// </summary>
    public async Task SetContentEditable(ElementReference element, ContentEditable value)
        => await js.ElementSetContentEditable(element, value);

    /// <summary>
    /// Returns a boolean value indicating whether or not the content of the element can be edited.
    /// </summary>
    public async Task<bool> IsContentEditable(ElementReference element)
        => await js.ElementIsContentEditable(element);

    /// <summary>
    /// The HTMLElement.dir property gets or sets the text writing directionality of the content of the current element.
    /// </summary>
    public async Task<ElementDir> GetDir(ElementReference element)
        => await js.ElementGetDir(element);
    /// <summary>
    /// The HTMLElement.dir property gets or sets the text writing directionality of the content of the current element.
    /// </summary>
    public async Task SetDir(ElementReference element, ElementDir value)
        => await js.ElementSetDir(element, value);

    /// <summary>
    /// The enterKeyHint property is an enumerated property defining what action label (or icon) 
    /// to present for the enter key on virtual keyboards.
    /// </summary>
    public async Task<EnterKeyHint> GetEnterKeyHint(ElementReference element)
        => await js.ElementGetEnterKeyHint(element);
    /// <summary>
    /// The enterKeyHint property is an enumerated property defining what action label (or icon) 
    /// to present for the enter key on virtual keyboards.
    /// </summary>
    public async Task SetEnterKeyHint(ElementReference element, EnterKeyHint value)
        => await js.ElementSetEnterKeyHint(element, value);

    /// <summary>
    /// The HTMLElement property hidden reflects the value of the element's hidden attribute.
    /// </summary>
    public async Task<Hidden> GetHidden(ElementReference element)
        => await js.ElementGetHidden(element);
    /// <summary>
    /// The HTMLElement property hidden reflects the value of the element's hidden attribute.
    /// </summary>
    public async Task SetHidden(ElementReference element, Hidden value)
        => await js.ElementSetHidden(element, value);

    /// <summary>
    /// The HTMLElement property inert reflects the value of the element's inert attribute. It is a boolean value that, when present, 
    /// makes the browser "ignore" user input events for the element, including focus events and events from assistive technologies.
    /// </summary>
    public async Task<bool> GetInert(ElementReference element)
        => await js.ElementGetInert(element);
    /// <summary>
    /// The HTMLElement property inert reflects the value of the element's inert attribute. It is a boolean value that, when present, 
    /// makes the browser "ignore" user input events for the element, including focus events and events from assistive technologies.
    /// </summary>
    public async Task SetInert(ElementReference element, bool value)
        => await js.ElementSetInert(element, value);

    /// <summary>
    /// The innerText property of the HTMLElement interface represents the rendered text content of a node and its descendants.
    /// </summary>
    public async Task<string> GetInnerText(ElementReference element)
        => await js.ElementGetInnerText(element);
    /// <summary>
    /// The innerText property of the HTMLElement interface represents the rendered text content of a node and its descendants.
    /// </summary>
    public async Task SetInnerText(ElementReference element, string value)
        => await js.ElementSetInnerText(element, value);

    /// <summary>
    /// The HTMLElement property inputMode reflects the value of the element's inputmode attribute.
    /// </summary>
    public async Task<InputMode> GetInputMode(ElementReference element)
        => await js.ElementGetInputMode(element);
    /// <summary>
    /// The HTMLElement property inputMode reflects the value of the element's inputmode attribute.
    /// </summary>
    public async Task SetInputMode(ElementReference element, InputMode value)
        => await js.ElementSetInputMode(element, value);

    /// <summary>
    /// The HTMLElement.offsetHeight read-only property returns the height of an element, including vertical padding and borders in px.
    /// </summary>
    public async Task<float> GetOffsetHeight(ElementReference element)
        => await js.ElementGetOffsetHeight(element);

    /// <summary>
    /// The HTMLElement.offsetLeft read-only property returns the number of pixels that the upper left corner 
    /// of the current element is offset to the left within the HTMLElement.offsetParent node.
    /// </summary>
    public async Task<float> GetOffsetLeft(ElementReference element)
        => await js.ElementGetOffsetLeft(element);

    /// <summary>
    /// The HTMLElement.offsetTop read-only property returns the distance from the outer border of the current element 
    /// (including its margin) to the top padding edge of the offsetParent, the closest positioned ancestor element.
    /// </summary>
    public async Task<float> GetOffsetTop(ElementReference element)
        => await js.ElementGetOffsetTop(element);

    /// <summary>
    /// The layout width of an element in px.
    /// </summary>
    public async Task<float> GetOffsetWidth(ElementReference element)
        => await js.ElementGetOffsetWidth(element);

    /// <summary>
    /// A number representing the position of the element in the tabbing order.
    /// </summary>
    public async Task<int> GetTabIndex(ElementReference element)
        => await js.ElementGetTabIndex(element);
    /// <summary>
    /// A number representing the position of the element in the tabbing order.
    /// </summary>
    public async Task SetTabIndex(ElementReference element, int value)
        => await js.ElementSetTabIndex(element, value);
}
