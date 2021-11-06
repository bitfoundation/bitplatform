using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    public static class JsRuntimeExtension
    {
        public static async Task SetToggleBodyOverflow(this IJSRuntime jsRuntime, bool isNavOpen)
        {
            await jsRuntime.InvokeVoidAsync("toggleBodyOverflow", isNavOpen);
        }

        public static async Task ScrollToFragmentOnClickEvent(this IJSRuntime jsRuntime, string targetElementId)
        {
            await jsRuntime.InvokeVoidAsync("scrollToFragmentOnClickEvent", targetElementId);
        }
        
        public static async Task CopyToClipboardOnClickEvent(this IJSRuntime jsRuntime, string codeSampleContentForCopy)
        {
            await jsRuntime.InvokeVoidAsync("copyToClipboardOnClickEvent", codeSampleContentForCopy);
        }
    }
}
