using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class JsRuntimeExtension
{
    internal static async Task SetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object value)
    {
        await jsRuntime.InvokeVoidAsync("Bit.setProperty", element, property, value);
    }

    internal static async Task<string> GetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
    {
        return await jsRuntime.InvokeAsync<string>("Bit.getProperty", element, property);
    }

    internal static async Task<int> GetClientHeight(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<int>("Bit.getClientHeight", element);
    }
    internal static async Task<IJSObjectReference?> InitUploaderDropZone(this IJSRuntime jsRuntime, ElementReference dragDropZoneElement, ElementReference inputFileElement)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference?>("BitFileUploader.initDropZone", dragDropZoneElement, inputFileElement);
    }
    internal static async Task<BitFileInfo[]?> InitUploader(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<BitFileUpload>? dotnetObjectReference, string uploadAddress, IReadOnlyDictionary<string, string> uploadRequestHttpHeaders)
    {
        if (uploadAddress.HasNoValue() || dotnetObjectReference is null) return null;

        return await jsRuntime.InvokeAsync<BitFileInfo[]>("BitFileUploader.init", element, dotnetObjectReference, uploadAddress, uploadRequestHttpHeaders);
    }

    internal static async Task UploadFile(this IJSRuntime jsRuntime, long to, long from, int index = -1)
    {
        await jsRuntime.InvokeVoidAsync("BitFileUploader.upload", index, to, from);
    }

    internal static async Task PauseFile(this IJSRuntime jsRuntime, int index = -1)
    {
        await jsRuntime.InvokeVoidAsync("BitFileUploader.pause", index);
    }

    internal static async Task<BoundingClientRect> GetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return await jsRuntime.InvokeAsync<BoundingClientRect>("Bit.getBoundingClientRect", element);
    }

    internal static async Task<string> RegisterOnWindowMouseUpEvent(this IJSRuntime jsRuntime, BitComponentBase dontetHelper, string callbackName)
    {

        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseUpEvent", DotNetObjectReference.Create(dontetHelper),
            callbackName);
    }
    internal static async Task<string> RegisterOnWindowMouseMoveEvent(this IJSRuntime jsRuntime, BitComponentBase dontetHelper, string callbackName)
    {
        return await jsRuntime.InvokeAsync<string>("BitColorPicker.registerOnWindowMouseMoveEvent", DotNetObjectReference.Create(dontetHelper),
             callbackName);
    }

    internal static async Task AbortProcedure(this IJSRuntime jSRuntime, string abortControllerId)
    {
        await jSRuntime.InvokeVoidAsync("BitColorPicker.abortProcedure", abortControllerId);
    }

    internal static async Task BitLinkScrollToFragmentOnClickEvent(this IJSRuntime jsRuntime, string targetElementId)
    {
        await jsRuntime.InvokeVoidAsync("BitLink.scrollToFragmentOnClickEvent", targetElementId);
    }

    internal static async Task SelectText(this IJSRuntime jsRuntime, ElementReference element)
    {
        await jsRuntime.InvokeVoidAsync("Bit.selectText", element);
    }
}
