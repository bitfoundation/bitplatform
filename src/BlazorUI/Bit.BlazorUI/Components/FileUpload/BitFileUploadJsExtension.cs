using System.Diagnostics.CodeAnalysis;
using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitFileUploadJsExtension
{
    internal static async Task<IJSObjectReference> SetupFileUploadDropzone(this IJSRuntime jsRuntime, ElementReference dragDropZoneElement, ElementReference inputFileElement)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference>("BitFileUpload.setupDropzone", dragDropZoneElement, inputFileElement);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static async Task<BitFileInfo[]?> ResetFileUpload(this IJSRuntime jsRuntime,
                                                                   Guid id,
                                                                   DotNetObjectReference<BitFileUpload>? dotnetObjectReference,
                                                                   ElementReference element,
                                                                   string uploadAddress,
                                                                   IReadOnlyDictionary<string, string> uploadRequestHttpHeaders)
    {
        return await jsRuntime.InvokeAsync<BitFileInfo[]>("BitFileUpload.reset", id.ToString(), dotnetObjectReference, element, uploadAddress, uploadRequestHttpHeaders);
    }

    internal static async Task UploadFile(this IJSRuntime jsRuntime, Guid id, long from, long to, int index, string? uploadUrl, IReadOnlyDictionary<string, string>? httpHeaders)
    {
        await (httpHeaders is null ? jsRuntime.InvokeVoidAsync("BitFileUpload.upload", id.ToString(), from, to, index, uploadUrl)
                                   : jsRuntime.InvokeVoidAsync("BitFileUpload.upload", id.ToString(), from, to, index, uploadUrl, httpHeaders));
    }

    internal static async Task PauseFile(this IJSRuntime jsRuntime, Guid id, int index = -1)
    {
        await jsRuntime.InvokeVoidAsync("BitFileUpload.pause", id.ToString(), index);
    }

    internal static async Task BrowseFile(this IJSRuntime jsRuntime, ElementReference inputFileElement)
    {
        await jsRuntime.InvokeVoidAsync("BitFileUpload.browse", inputFileElement);
    }

    internal static async Task DisposeFileUpload(this IJSRuntime jsRuntime, Guid id)
    {
        await jsRuntime.InvokeVoidAsync("BitFileUpload.dispose", id.ToString());
    }
}
