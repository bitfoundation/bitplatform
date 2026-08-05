using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitFileUploadJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInfo))]
    internal static ValueTask<BitFileInfo[]> BitFileUploadSetup(this IJSRuntime jsRuntime,
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
        return jsRuntime.Invoke<BitFileInfo[]>("BitBlazorUI.FileUpload.setup",
                                               id, dotnetObjectReference, element, append, uploadAddress, uploadRequestHttpHeaders,
                                               method, withCredentials, timeout, fieldName, showPreview, readImageDimensions);
    }

    internal static ValueTask BitFileUploadRelease(this IJSRuntime jsRuntime, string id, int index)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.release", id, index);
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
            return jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl, httpHeaders ?? [], formFields);
        }

        return (httpHeaders is null ? jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl)
                                    : jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.upload", id, from, to, index, uploadUrl, httpHeaders));
    }

    internal static ValueTask BitFileUploadPause(this IJSRuntime jsRuntime, string id, int index = -1)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.pause", id, index);
    }

    internal static ValueTask<IJSObjectReference> BitFileUploadSetupDragDrop(this IJSRuntime jsRuntime,
                                                                                  ElementReference dragDropZoneElement,
                                                                                  ElementReference inputFileElement,
                                                                                  string dragClass,
                                                                                  string? dragStyle,
                                                                                  bool allowDrop,
                                                                                  bool allowPaste,
                                                                                  bool expandDirectories)
    {
        return jsRuntime.Invoke<IJSObjectReference>("BitBlazorUI.FileUpload.setupDragDrop",
                                                    dragDropZoneElement, inputFileElement, dragClass, dragStyle,
                                                    allowDrop, allowPaste, expandDirectories);
    }

    internal static ValueTask BitFileUploadBrowse(this IJSRuntime jsRuntime, ElementReference inputFileElement)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.browse", inputFileElement);
    }

    internal static ValueTask BitFileUploadClear(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.clear", id);
    }

    internal static ValueTask BitFileUploadReset(this IJSRuntime jsRuntime, string id, ElementReference inputFileElement)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.FileUpload.reset", id, inputFileElement);
    }
}
