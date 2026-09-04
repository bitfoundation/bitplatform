using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitFileUploadJsRuntimeExtensions
{
    // The js side of the setup is asynchronous (it reads the image dimensions before it answers), so this
    // one stays on the regular asynchronous invocation - the fast in-process path would leave it running
    // and come back with nothing. Invoke still returns default (null) when the runtime can't service
    // interop, so the result is normalized to an empty array to keep callers (e.g. _files.AddRange(...))
    // from crashing with ArgumentNullException.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static async ValueTask<BitFileInfo[]> BitFileUploadSetup(this IJSRuntime jsRuntime,
                                                                     string id,
                                                                     DotNetObjectReference<BitFileUpload>? dotnetObjectReference,
                                                                     ElementReference element,
                                                                     bool append,
                                                                     string? uploadAddress,
                                                                     Dictionary<string, string>? uploadRequestHttpHeaders,
                                                                     string? method,
                                                                     bool withCredentials,
                                                                     long timeout,
                                                                     string? fieldName,
                                                                     bool showPreview,
                                                                     bool readImageDimensions)
    {
        const string identifier = "BitBlazorUI.FileUpload.setup";
        var result = await jsRuntime.Invoke<BitFileInfo[]>(identifier,
                                                           id, dotnetObjectReference, element, append, uploadAddress, uploadRequestHttpHeaders,
                                                           method, withCredentials, timeout, fieldName, showPreview, readImageDimensions);
        jsRuntime.ReportIfUnexpectedNull(identifier, result);
        return result ?? [];
    }

    internal static ValueTask BitFileUploadRelease(this IJSRuntime jsRuntime, string id, int index)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.release", id, index);
    }

    internal static ValueTask BitFileUploadUpload(this IJSRuntime jsRuntime,
                                                       string id,
                                                       long from,
                                                       long to,
                                                       int index,
                                                       string? uploadUrl,
                                                       Dictionary<string, string>? httpHeaders,
                                                       Dictionary<string, string>? formFields)
    {
        // the optional arguments are only left out when there is nothing to send, so that the
        // defaults of the JavaScript side keep applying instead of an explicit null overriding them.
        if (formFields is not null)
        {
            return jsRuntime.FastInvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl, httpHeaders ?? [], formFields);
        }

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
                                                                                         ElementReference inputFileElement,
                                                                                         string dragClass,
                                                                                         string? dragStyle,
                                                                                         bool allowDrop,
                                                                                         bool allowPaste,
                                                                                         bool expandDirectories)
    {
        const string identifier = "BitBlazorUI.FileUpload.setupDragDrop";
        var result = await jsRuntime.FastInvoke<IJSObjectReference>(identifier,
                                                                    dragDropZoneElement, inputFileElement, dragClass, dragStyle,
                                                                    allowDrop, allowPaste, expandDirectories);
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
