using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

public class Element(IJSRuntime js)
{
    /// <summary>
    /// Removes keyboard focus from the currently focused element.
    /// </summary>
    public async Task Blur(ElementReference element) => await js.ElementBlur(element);

    /// <summary>
    /// Retrieves the value of the named attribute from the current node and returns it as a string.
    /// </summary>
    public async Task<string> GetAttribute(ElementReference element, string name) => await js.ElementGetAttribute(element, name);

    /// <summary>
    /// Returns an array of attribute names from the current element.
    /// </summary>
    public async Task<string[]> GetAttributeNames(ElementReference element) => await js.ElementGetAttributeNames(element);

    /// <summary>
    /// Returns the size of an element and its position relative to the viewport.
    /// </summary>
    public async Task<DomRect> GetBoundingClientRect(ElementReference element) => await js.ElementGetBoundingClientRect(element);

    /// <summary>
    /// Releases (stops) pointer capture that was previously set for a specific pointer event.
    /// </summary>
    public async Task ReleasePointerCapture(ElementReference element, int pointerId) => await js.ElementReleasePointerCapture(element, pointerId);

    /// <summary>
    /// Removes the element from the children list of its parent.
    /// </summary>
    public async Task Remove(ElementReference element) => await js.ElementRemove(element);

    /// <summary>
    /// Removes the named attribute from the current node.
    /// </summary>
    public async Task<string> RemoveAttribute(ElementReference element, string name) => await js.ElementRemoveAttribute(element, name);

    /// <summary>
    /// Asynchronously asks the browser to make the element fullscreen.
    /// </summary>
    public async Task RequestFullScreen(ElementReference element, FullScreenOptions? options) => await js.ElementRequestFullScreen(element, options);

    /// <summary>
    /// Scrolls to a particular set of coordinates inside a given element.
    /// </summary>
    public async Task Scroll(ElementReference element, ScrollToOptions? options) => await js.ElementScroll(element, options, null, null);
    /// <summary>
    /// Scrolls to a particular set of coordinates inside a given element.
    /// </summary>
    public async Task Scroll(ElementReference element, double? x, double? y) => await js.ElementScroll(element, null, x, y);
    /// <summary>
    /// Scrolls to a particular set of coordinates inside a given element.
    /// </summary>
    public async Task Scroll(ElementReference element, ScrollToOptions? options, double? x, double? y) => await js.ElementScroll(element, options, x, y);

    /// <summary>
    /// Scrolls an element by the given amount.
    /// </summary>
    public async Task ScrollBy(ElementReference element, ScrollToOptions? options) => await js.ElementScrollBy(element, options, null, null);
    /// <summary>
    /// Scrolls an element by the given amount.
    /// </summary>
    public async Task ScrollBy(ElementReference element, double? x, double? y) => await js.ElementScrollBy(element, null, x, y);
    /// <summary>
    /// Scrolls an element by the given amount.
    /// </summary>
    public async Task ScrollBy(ElementReference element, ScrollToOptions? options, double? x, double? y) => await js.ElementScrollBy(element, options, x, y);

    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element) => await ScrollIntoView(element, null, null);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element, bool alignToTop) => await ScrollIntoView(element, alignToTop, null);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element, ScrollIntoViewOptions options) => await ScrollIntoView(element, null, options);
    /// <summary>
    /// Scrolls the page until the element gets into the view.
    /// </summary>
    public async Task ScrollIntoView(ElementReference element, bool? alignToTop, ScrollIntoViewOptions? options) => await js.ElementScrollIntoView(element, alignToTop, options);

    /// <summary>
    /// Sets the value of a named attribute of the current node.
    /// </summary>
    public async Task SetAttribute(ElementReference element, string name, string value) => await js.ElementSetAttribute(element, name, value);

    /// <summary>
    /// Designates a specific element as the capture target of future pointer events.
    /// </summary>
    public async Task SetPointerCapture(ElementReference element, int pointerId) => await js.ElementSetPointerCapture(element, pointerId);

    /// <summary>
    /// A string representing the access key assigned to the element.
    /// </summary>
    public async Task<string> GetAccessKey(ElementReference element) => await js.ElementGetAccessKey(element);
    /// <summary>
    /// A string representing the access key assigned to the element.
    /// </summary>
    public async Task SetAccessKey(ElementReference element, string key) => await js.ElementSetAccessKey(element, key);

}
