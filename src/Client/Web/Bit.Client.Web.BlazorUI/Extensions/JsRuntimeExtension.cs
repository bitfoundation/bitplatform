using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop
{
    internal static class JsRuntimeExtension
    {
        public static async Task SetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object value)
        {
            await jsRuntime.InvokeVoidAsync("Bit.setProperty", element, property, value).ConfigureAwait(false);
        }

        public static async Task<string> GetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
        {
            return await jsRuntime.InvokeAsync<string>("Bit.getProperty", element, property).ConfigureAwait(false);
        }

        public static async Task<int> GetClientHeight(this IJSRuntime jsRuntime, ElementReference element)
        {
            return await jsRuntime.InvokeAsync<int>("Bit.getClientHeight", element).ConfigureAwait(false);
        }

        public static async Task<BitFileInfo[]?> InitUploader(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<BitFileUpload>? dotnetObjectReference, string uploadAddress, IReadOnlyDictionary<string, string> uploadRequestHttpHeaders)
        {
            if (uploadAddress.HasNoValue() || dotnetObjectReference is null) return null;

            return await jsRuntime.InvokeAsync<BitFileInfo[]>("BitFileUploader.init", element, dotnetObjectReference, uploadAddress, uploadRequestHttpHeaders).ConfigureAwait(false);
        }

        public static async Task UploadFile(this IJSRuntime jsRuntime, long to, long from, int index = -1)
        {
            await jsRuntime.InvokeVoidAsync("BitFileUploader.upload", index, to, from).ConfigureAwait(false);
        }

        public static async Task PauseFile(this IJSRuntime jsRuntime, int index = -1)
        {
            await jsRuntime.InvokeVoidAsync("BitFileUploader.pause", index).ConfigureAwait(false);
        }

        public static async Task<BoundingClientRect> GetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
        {
            return await jsRuntime.InvokeAsync<BoundingClientRect>("Bit.getBoundingClientRect", element).ConfigureAwait(false);
        }

        public static async Task<string> RegisterOnWindowMouseUpEvent(this IJSRuntime jsRuntime, BitComponentBase dontetHelper, string callbackName)
        {

            return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseUpEvent", DotNetObjectReference.Create(dontetHelper), callbackName).ConfigureAwait(false);
        }
        public static async Task<string> RegisterOnWindowMouseMoveEvent(this IJSRuntime jsRuntime, BitComponentBase dontetHelper, string callbackName)
        {
            return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseMoveEvent", DotNetObjectReference.Create(dontetHelper), callbackName).ConfigureAwait(false);
        }

        public static async Task AbortProcedure(this IJSRuntime jSRuntime, string abortControllerId)
        {
            await jSRuntime.InvokeVoidAsync("BitColorPicker.abortProcedure", abortControllerId).ConfigureAwait(false);
        }

        public static async Task BitLinkScrollToFragmentOnClickEvent(this IJSRuntime jsRuntime, string targetElementId)
        {
            await jsRuntime.InvokeVoidAsync("BitLink.scrollToFragmentOnClickEvent", targetElementId).ConfigureAwait(false);
        }

        public static async Task SelectText(this IJSRuntime jsRuntime, ElementReference element)
        {
            await jsRuntime.InvokeVoidAsync("Bit.selectText", element).ConfigureAwait(false);
        }
    }

    public class BoundingClientRect
    {
        public double Bottom { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
