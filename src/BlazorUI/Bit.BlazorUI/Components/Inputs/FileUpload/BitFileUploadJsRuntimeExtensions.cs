using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitFileUploadJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static async ValueTask<BitFileInfo[]> BitFileUploadSetup(this IJSRuntime jsRuntime,
                                                                     string id,
                                                                     DotNetObjectReference<BitFileUpload>? dotnetObjectReference,
                                                                     ElementReference element,
                                                                     bool append,
                                                                     string? uploadAddress,
                                                                     Dictionary<string, string>? uploadRequestHttpHeaders)
    {
        // FastInvoke returns default (null) when the runtime can't service interop or a JSON/JS interop
        // error is swallowed on the in-process (WASM) path, so normalize to an empty array to keep callers
        // (e.g. _files.AddRange(...)) from crashing with ArgumentNullException.
        const string identifier = "BitBlazorUI.FileUpload.setup";
        var result = await jsRuntime.FastInvoke<BitFileInfo[]>(identifier, id, dotnetObjectReference, element, append, uploadAddress, uploadRequestHttpHeaders);
        jsRuntime.ReportIfUnexpectedNull(identifier, result);
        return result ?? [];
    }

    internal static ValueTask BitFileUploadUpload(this IJSRuntime jsRuntime,
                                                       string id,
                                                       long from,
                                                       long to,
                                                       int index,
                                                       string? uploadUrl,
                                                       Dictionary<string, string>? httpHeaders)
    {
        return (httpHeaders is null ? jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl)
                                    : jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl, httpHeaders));
    }

    internal static ValueTask BitFileUploadPause(this IJSRuntime jsRuntime, string id, int index = -1)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.pause", id, index);
    }

    // FastInvoke returns default (null) when the runtime can't service interop or a JSON/JS interop
    // error is swallowed on the in-process (WASM) path. Callers must null-check before using the
    // reference; a null result means drag/drop was not initialized.
    internal static async ValueTask<IJSObjectReference?> BitFileUploadSetupDragDrop(this IJSRuntime jsRuntime,
                                                                                         ElementReference dragDropZoneElement,
                                                                                         ElementReference inputFileElement)
    {
        const string identifier = "BitBlazorUI.FileUpload.setupDragDrop";
        var result = await jsRuntime.FastInvoke<IJSObjectReference>(identifier, dragDropZoneElement, inputFileElement);
        return jsRuntime.ReportIfUnexpectedNull(identifier, result);
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
