using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

public static class WebSiteJsRuntimeExtension
{
    public static async Task SetToggleBodyOverflow(this IJSRuntime jsRuntime, bool isNavOpen)
    {
        await jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
    }

    public static async Task ScrollToElement(this IJSRuntime jsRuntime, string targetElementId)
    {
        await jsRuntime.InvokeVoidAsync("scrollToElement", targetElementId);
    }

    public static async Task CopyToClipboard(this IJSRuntime jsRuntime, string codeSampleContentForCopy)
    {
        await jsRuntime.InvokeVoidAsync("copyToClipboard", codeSampleContentForCopy);
    }

    public static async Task RegisterOnScrollToChangeHeaderStyle(this IJSRuntime jsRuntime, ElementReference element)
    {
        await jsRuntime.InvokeVoidAsync("RegisterOnScrollToChangeHeaderStyle", element);
    }
}
