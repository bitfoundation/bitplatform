using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

public class Element(IJSRuntime js)
{
    public async Task Blur(ElementReference element) => await js.ElementBlur(element);

    public async Task<string> GetAttribute(ElementReference element, string name) => await js.ElementGetAttribute(element, name);

    public async Task<string[]> GetAttributeNames(ElementReference element) => await js.ElementGetAttributeNames(element);

    public async Task<DomRect> GetBoundingClientRect(ElementReference element) => await js.ElementGetBoundingClientRect(element);

    public async Task ReleasePointerCapture(ElementReference element, int pointerId) => await js.ElementReleasePointerCapture(element, pointerId);

    public async Task Remove(ElementReference element) => await js.ElementRemove(element);

    public async Task<string> RemoveAttribute(ElementReference element, string name) => await js.ElementRemoveAttribute(element, name);

    public async Task RequestFullScreen(ElementReference element, FullScreenOptions? options) => await js.ElementRequestFullScreen(element, options);

    public async Task Scroll(ElementReference element, ScrollToOptions? options) => await js.ElementScroll(element, options, null, null);
    public async Task Scroll(ElementReference element, double? x, double? y) => await js.ElementScroll(element, null, x, y);
    public async Task Scroll(ElementReference element, ScrollToOptions? options, double? x, double? y) => await js.ElementScroll(element, options, x, y);

    public async Task ScrollBy(ElementReference element, ScrollToOptions? options) => await js.ElementScrollBy(element, options, null, null);
    public async Task ScrollBy(ElementReference element, double? x, double? y) => await js.ElementScrollBy(element, null, x, y);
    public async Task ScrollBy(ElementReference element, ScrollToOptions? options, double? x, double? y) => await js.ElementScrollBy(element, options, x, y);

    public async Task ScrollIntoView(ElementReference element) => await ScrollIntoView(element, null, null);
    public async Task ScrollIntoView(ElementReference element, bool alignToTop) => await ScrollIntoView(element, alignToTop, null);
    public async Task ScrollIntoView(ElementReference element, ScrollIntoViewOptions options) => await ScrollIntoView(element, null, options);
    public async Task ScrollIntoView(ElementReference element, bool? alignToTop, ScrollIntoViewOptions? options) => await js.ElementScrollIntoView(element, alignToTop, options);

    public async Task SetAttribute(ElementReference element, string name, string value) => await js.ElementSetAttribute(element, name, value);

    public async Task SetPointerCapture(ElementReference element, int pointerId) => await js.ElementSetPointerCapture(element, pointerId);
}
