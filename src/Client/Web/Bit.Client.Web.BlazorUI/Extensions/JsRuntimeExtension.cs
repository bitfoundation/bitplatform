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

        public static async Task FillRatingComponentIcons(this IJSRuntime jsRuntime, ElementReference element, int index, IconSide side, string activeColor, string emptyColor)
        {
            await jsRuntime.InvokeVoidAsync("Bit.fillRatingComponentIcons", element, index, "left", activeColor, emptyColor);
        }
    }
}
