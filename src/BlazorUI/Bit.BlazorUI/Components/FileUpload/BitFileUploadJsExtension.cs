using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;

namespace Microsoft.JSInterop;

internal static class BitFileUploadJsExtension
{
    internal static async Task<IJSObjectReference?> SetupFileUploadDropzone(this IJSRuntime jsRuntime, ElementReference dragDropZoneElement, ElementReference inputFileElement)
    {
        return await jsRuntime.InvokeAsync<IJSObjectReference?>("BitFileUpload.setupDropzone", dragDropZoneElement, inputFileElement);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static async Task<BitFileInfo[]?> InitFileUpload(this IJSRuntime jsRuntime, ElementReference element, DotNetObjectReference<BitFileUpload>? dotnetObjectReference, string uploadAddress, IReadOnlyDictionary<string, string> uploadRequestHttpHeaders)
    {
        if (uploadAddress.HasNoValue() || dotnetObjectReference is null) return null;

        return await jsRuntime.InvokeAsync<BitFileInfo[]>("BitFileUpload.init", element, dotnetObjectReference, uploadAddress, uploadRequestHttpHeaders);
    }

    internal static async Task UploadFile(this IJSRuntime jsRuntime, long from, long to, int index = -1)
    {
        await jsRuntime.InvokeVoidAsync("BitFileUpload.upload", from, to, index);
    }

    internal static async Task PauseFile(this IJSRuntime jsRuntime, int index = -1)
    {
        await jsRuntime.InvokeVoidAsync("BitFileUpload.pause", index);
    }
}
