using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    public static class JsRuntimeExtension
    {
        public static async Task SetElementProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object value)
        {
            await jsRuntime.InvokeVoidAsync("setElementProperty", element, property, value);
        }

        public static async Task<string> GetElementProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
        {
            return await jsRuntime.InvokeAsync<string>("getElementProperty", element, property);
        }
    }
}
