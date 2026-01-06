using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitFileInputJsRuntimeExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInputInfo))]
    internal static ValueTask<BitFileInputInfo[]> BitFileInputSetup(this IJSRuntime jsRuntime,
                                                                     string id,
                                                                     ElementReference element,
                                                                     bool append)
    {
        return jsRuntime.InvokeAsync<BitFileInputInfo[]>("BitBlazorUI.FileInput.setup", id, element, append);
    }

    internal static ValueTask<IJSObjectReference> BitFileInputSetupDragDrop(this IJSRuntime jsRuntime,
                                                                            ElementReference dragDropZoneElement,
                                                                            ElementReference inputFileElement)
    {
        return jsRuntime.InvokeAsync<IJSObjectReference>("BitBlazorUI.FileInput.setupDragDrop", dragDropZoneElement, inputFileElement);
    }

    internal static ValueTask BitFileInputBrowse(this IJSRuntime jsRuntime, ElementReference inputFileElement)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileInput.browse", inputFileElement);
    }

    internal static ValueTask BitFileInputClear(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileInput.clear", id);
    }

    internal static ValueTask BitFileInputReset(this IJSRuntime jsRuntime, string id, ElementReference inputFileElement)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.FileInput.reset", id, inputFileElement);
    }
}
