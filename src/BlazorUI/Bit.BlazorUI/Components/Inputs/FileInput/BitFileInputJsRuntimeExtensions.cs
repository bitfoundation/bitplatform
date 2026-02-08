using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Provides JavaScript interop extension methods for the <see cref="BitFileInput"/> component.
/// </summary>
internal static class BitFileInputJsRuntimeExtensions
{
    /// <summary>
    /// Initializes the file input on the JavaScript side, registers selected files, and returns their metadata.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInputInfo))]
    internal static ValueTask<BitFileInputInfo[]> BitFileInputSetup(this IJSRuntime jsRuntime,
                                                                     string id,
                                                                     ElementReference element,
                                                                     bool append)
    {
        return jsRuntime.InvokeAsync<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", id, element, append);
    }

    /// <summary>
    /// Sets up drag-and-drop and paste event handlers on the drop zone element to forward files to the input element.
    /// </summary>
    internal static ValueTask<IJSObjectReference> BitFileInputSetupDragDrop(this IJSRuntime jsRuntime,
                                                                            ElementReference dragDropZoneElement,
                                                                            ElementReference inputFileElement)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.FileInput.setupDragDrop", dragDropZoneElement, inputFileElement);
    }

    /// <summary>
    /// Programmatically triggers a click on the file input element to open the file browser dialog.
    /// </summary>
    internal static ValueTask BitFileInputBrowse(this IJSRuntime jsRuntime, ElementReference inputFileElement)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileInput.browse", inputFileElement);
    }

    /// <summary>
    /// Removes all stored file references for the given component from the JavaScript side.
    /// </summary>
    internal static ValueTask BitFileInputClear(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileInput.clear", id);
    }

    /// <summary>
    /// Clears all stored file references and resets the file input element's value.
    /// </summary>
    internal static ValueTask BitFileInputReset(this IJSRuntime jsRuntime, string id, ElementReference inputFileElement)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileInput.reset", id, inputFileElement);
    }

    /// <summary>
    /// Reads the content of a specific file from the JavaScript file store and returns it as a byte array.
    /// </summary>
    internal static ValueTask<byte[]> BitFileInputReadContent(this IJSRuntime jsRuntime, string id, string fileId)
    {
        return jsRuntime.InvokeAsync<byte[]>("BitBlazorUI.FileInput.readContent", id, fileId);
    }
}
