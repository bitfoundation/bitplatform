namespace Bit.Butil;

/// <summary>
/// Temporary shim: DELETE this file (and index.ts's <c>clickElement</c> bridge) once Bit.Butil ships
/// <c>ElementReferenceExtensions.Click</c> - the real extension then takes over at every call site.
/// </summary>
public static class ButilElementReferenceExtensions
{
    extension(ElementReference element)
    {
        /// <summary>
        /// Sends a click to the element, as pressing it would.
        /// <br />
        /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/click">https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/click</see>
        /// </summary>
        public async Task Click()
        {
            await GetJs(element).InvokeVoidAsync("clickElement", element);
        }
    }

    // The accessor Butil's own ElementReferenceExtensions uses: the property getter rather than the synthesized
    // backing field, whose name is an implementation detail.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_JSRuntime")]
    private static extern IJSRuntime JSRuntimeGetter(WebElementReferenceContext context);

    private static IJSRuntime GetJs(ElementReference element)
    {
        var context = element.Context as WebElementReferenceContext
            ?? throw new InvalidOperationException("ElementReference has not been configured correctly.");

        return JSRuntimeGetter(context);
    }
}
