using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitFileUploadJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static ValueTask<BitFileInfo[]> BitFileUploadSetup(this IJSRuntime jsRuntime,
                                                                     string id,
                                                                     DotNetObjectReference<BitFileUpload>? dotnetObjectReference,
                                                                     ElementReference element,
                                                                     bool append,
                                                                     string uploadAddress,
                                                                     IReadOnlyDictionary<string, string> uploadRequestHttpHeaders)
    {
        return jsRuntime.FastInvoke<BitFileInfo[]>("BitBlazorUI.FileUpload.setup", id, dotnetObjectReference, element, append, uploadAddress, uploadRequestHttpHeaders);
    }

    internal static ValueTask BitFileUploadUpload(this IJSRuntime jsRuntime,
                                                       string id,
                                                       long from,
                                                       long to,
                                                       int index,
                                                       string? uploadUrl,
                                                       IReadOnlyDictionary<string, string>? httpHeaders)
    {
        return (httpHeaders is null ? jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl)
                                    : jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl, httpHeaders));
    }

    internal static ValueTask BitFileUploadPause(this IJSRuntime jsRuntime, string id, int index = -1)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.pause", id, index);
    }

    internal static ValueTask<IJSObjectReference> BitFileUploadSetupDragDrop(this IJSRuntime jsRuntime,
                                                                                  ElementReference dragDropZoneElement,
                                                                                  ElementReference inputFileElement)
    {
        return jsRuntime.FastInvoke<IJSObjectReference>("BitBlazorUI.FileUpload.setupDragDrop", dragDropZoneElement, inputFileElement);
    }

    internal static ValueTask BitFileUploadBrowse(this IJSRuntime jsRuntime, ElementReference inputFileElement)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.browse", inputFileElement);
    }

    internal static ValueTask BitFileUploadClear(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.clear", id);
    }

    internal static ValueTask BitFileUploadReset(this IJSRuntime jsRuntime, string id, ElementReference inputFileElement)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.reset", id, inputFileElement);
    }
}
