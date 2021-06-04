using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop
{
    public static class JsRuntimeExtension
    {
        public static async Task SetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object value)
        {
            await jsRuntime.InvokeVoidAsync("Bit.setProperty", element, property, value);
        }

        public static async Task<string> GetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
        {
            return await jsRuntime.InvokeAsync<string>("Bit.getProperty", element, property);
        }

        //public static async Task<decimal> GetHeight(this IJSRuntime jsRuntime, string selector)
        //{
        //    return await jsRuntime.InvokeAsync<decimal>("Bit.getHeight", selector);
        //}

        //public static async Task AddClass(this IJSRuntime jsRuntime, string selector, string className)
        //{
        //    await jsRuntime.InvokeVoidAsync("Bit.addClass", selector, className);
        //}
    }
}
