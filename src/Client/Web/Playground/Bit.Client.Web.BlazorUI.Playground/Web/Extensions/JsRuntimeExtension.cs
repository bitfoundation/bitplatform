using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    public static class JsRuntimeExtension
    {
        public static async Task ShowHideNavbar(this IJSRuntime jsRuntime)
        {
            await jsRuntime.InvokeVoidAsync("showHideNavbar");
        }
    }
}
