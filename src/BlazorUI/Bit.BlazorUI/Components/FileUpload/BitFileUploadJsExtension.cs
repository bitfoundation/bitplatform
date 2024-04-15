using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitFileUploadJsExtension
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static async Task<BitFileInfo[]?> BitFileUploadReset(this IJSRuntime jsRuntime,
                                                                   Guid id,
                                                                   DotNetObjectReference<BitFileUpload>? dotnetObjectReference,
                                                                   ElementReference element,
                                                                   string uploadAddress,
                                                                   IReadOnlyDictionary<string, string> uploadRequestHttpHeaders)
    {
        return await jsRuntime.InvokeAsync<BitFileInfo[]>("BitBlazorUI.FileUpload.reset", id.ToString(), dotnetObjectReference, element, uploadAddress, uploadRequestHttpHeaders);
    }

    internal static async Task BitFileUploadUpload(this IJSRuntime jsRuntime, Guid id, long from, long to, int index, string? uploadUrl, IReadOnlyDictionary<string, string>? httpHeaders)
    {
        await (httpHeaders is null ? jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.upload", id.ToString(), from, to, index, uploadUrl)
                                   : jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.upload", id.ToString(), from, to, index, uploadUrl, httpHeaders));
    }

    internal static async Task BitFileUploadPause(this IJSRuntime jsRuntime, Guid id, int index = -1)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.pause", id.ToString(), index);
    }

    internal static async Task<IJSObjectReference> BitFileUploadSetupDragDrop(this IJSRuntime jsRuntime, ElementReference dragDropZoneElement, ElementReference inputFileElement)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.FileUpload.setupDragDrop", dragDropZoneElement, inputFileElement);
    }

    internal static async Task BitFileUploadBrowse(this IJSRuntime jsRuntime, ElementReference inputFileElement)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.browse", inputFileElement);
    }

    internal static async Task BitFileUploadDispose(this IJSRuntime jsRuntime, Guid id)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.FileUpload.dispose", id.ToString());
    }
}
