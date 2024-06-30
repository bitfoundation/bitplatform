using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitFileUploadJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static ValueTask<BitFileInfo[]> BitFileUploadReset(this IJSRuntime jsRuntime,
                                                                     string id,
                                                                     DotNetObjectReference<BitFileUpload>? dotnetObjectReference,
                                                                     ElementReference element,
                                                                     string uploadAddress,
                                                                     IReadOnlyDictionary<string, string> uploadRequestHttpHeaders)
    {
        return jsRuntime.InvokeAsync<BitFileInfo[]>("BitBlazorUI.FileUpload.reset", id, dotnetObjectReference, element, uploadAddress, uploadRequestHttpHeaders);
    }

    internal static ValueTask BitFileUploadUpload(this IJSRuntime jsRuntime, 
                                                       string id, 
                                                       long from, 
                                                       long to, 
                                                       int index, 
                                                       string? uploadUrl, 
                                                       IReadOnlyDictionary<string, string>? httpHeaders)
    {
        return (httpHeaders is null ? jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl)
                                    : jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl, httpHeaders));
    }

    internal static ValueTask BitFileUploadPause(this IJSRuntime jsRuntime, string id, int index = -1)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.pause", id, index);
    }

    internal static ValueTask<IJSObjectReference> BitFileUploadSetupDragDrop(this IJSRuntime jsRuntime, 
                                                                                  ElementReference dragDropZoneElement, 
                                                                                  ElementReference inputFileElement)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.FileUpload.setupDragDrop", dragDropZoneElement, inputFileElement);
    }

    internal static ValueTask BitFileUploadBrowse(this IJSRuntime jsRuntime, ElementReference inputFileElement)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.browse", inputFileElement);
    }

    internal static ValueTask BitFileUploadDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.dispose", id);
    }
}
