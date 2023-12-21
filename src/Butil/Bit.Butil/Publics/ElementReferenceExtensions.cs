using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace Bit.Butil;

public static class ElementReferenceExtensions
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_JSRuntime")]
    extern static IJSRuntime JSRuntimeGetter(WebElementReferenceContext context);

    private static IJSRuntime GetJSRuntime(ElementReference elementReference)
    {
        var context = (elementReference.Context as WebElementReferenceContext) ?? throw new InvalidOperationException("ElementReference has not been configured correctly.");
        return JSRuntimeGetter(context);
    }


    public static ValueTask Blur(this ElementReference element)
    {
        return GetJSRuntime(element).InvokeVoidAsync("BitButil.element.blur", element);
    }

    public static ValueTask<Rect> GetBoundingClientRect(this ElementReference element)
    {
        //_ = element.FocusAsync();
        return GetJSRuntime(element).InvokeAsync<Rect>("BitButil.element.getBoundingClientRect", element);
    }
}
