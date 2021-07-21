using System;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI;
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

        public static async Task<BitFileInfo[]?> InitUploader(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<BitFileUpload>? dotnetObjectReference, Uri UploadAddress)
        {
            if (UploadAddress is null || dotnetObjectReference is null) return null;
            return await jsRuntime.InvokeAsync<BitFileInfo[]>("BitFileUploader.init", element, dotnetObjectReference, UploadAddress.AbsoluteUri);
        }

        public static async Task UploadFile(this IJSRuntime jsRuntime, long to, long from, int index = -1)
        {
            await jsRuntime.InvokeVoidAsync("BitFileUploader.upload", index, to, from);
        }

        public static async Task PauseFile(this IJSRuntime jsRuntime, int index = -1)
        {
            await jsRuntime.InvokeVoidAsync("BitFileUploader.pause", index);
        }

        public static async Task<int> GetClientHeight(this IJSRuntime jsRuntime, ElementReference element)
        {
            return await jsRuntime.InvokeAsync<int>("Bit.getClientHeight", element);
        }
    }
}
