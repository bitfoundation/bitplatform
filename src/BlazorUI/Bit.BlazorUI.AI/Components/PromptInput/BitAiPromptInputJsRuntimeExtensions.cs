namespace Bit.BlazorUI;

internal static class BitAiPromptInputJsRuntimeExtensions
{
    internal static ValueTask BitAiPromptInputAutoResize(this IJSRuntime jsRuntime, ElementReference textarea, int maxRows)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.autoResize", textarea, maxRows);
    }

    internal static ValueTask BitAiPromptInputFocus(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.focus", element);
    }

    internal static ValueTask BitAiPromptInputClick(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.click", element);
    }

    internal static ValueTask<string?> BitAiPromptInputPreviewFromInput(this IJSRuntime jsRuntime, ElementReference input, int index, string name)
    {
        return jsRuntime.Invoke<string?>("BitBlazorUI.AiPromptInput.previewFromInput", input, index, name);
    }

    internal static ValueTask BitAiPromptInputRevokePreview(this IJSRuntime jsRuntime, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.revokePreview", url);
    }

    internal static ValueTask BitAiPromptInputRegisterPaste(this IJSRuntime jsRuntime, string id, ElementReference element, DotNetObjectReference<BitAiPromptInput> dotnetObj, long maxSize, bool disabled)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.registerPaste", id, element, dotnetObj, maxSize, disabled);
    }

    internal static ValueTask BitAiPromptInputUnregisterPaste(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.unregisterPaste", id);
    }

    internal static ValueTask BitAiPromptInputRegisterDrop(this IJSRuntime jsRuntime, string id, ElementReference element, DotNetObjectReference<BitAiPromptInput> dotnetObj, long maxSize, bool disabled)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.registerDrop", id, element, dotnetObj, maxSize, disabled);
    }

    internal static ValueTask BitAiPromptInputUnregisterDrop(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.unregisterDrop", id);
    }

    internal static ValueTask<int?> BitAiPromptInputGetCaret(this IJSRuntime jsRuntime, ElementReference textarea)
    {
        return jsRuntime.Invoke<int?>("BitBlazorUI.AiPromptInput.getCaret", textarea);
    }

    internal static ValueTask BitAiPromptInputSetCaret(this IJSRuntime jsRuntime, ElementReference textarea, int position)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.setCaret", textarea, position);
    }

    internal static ValueTask<string?> BitAiPromptInputReadDraft(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.Invoke<string?>("BitBlazorUI.AiPromptInput.readDraft", key);
    }

    internal static ValueTask BitAiPromptInputWriteDraft(this IJSRuntime jsRuntime, string key, string text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.writeDraft", key, text);
    }

    internal static ValueTask BitAiPromptInputClearDraft(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.clearDraft", key);
    }

    internal static ValueTask BitAiPromptInputRegisterSubmitKey(this IJSRuntime jsRuntime, string id, ElementReference textarea, bool submitOnEnter)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.registerSubmitKey", id, textarea, submitOnEnter);
    }

    internal static ValueTask BitAiPromptInputUnregisterSubmitKey(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiPromptInput.unregisterSubmitKey", id);
    }
}
