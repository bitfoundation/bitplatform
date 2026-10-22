namespace Bit.BlazorUI;

internal static class BitAiVoiceInputJsRuntimeExtensions
{
    internal static ValueTask<bool> BitAiVoiceInputIsDictationSupported(this IJSRuntime jsRuntime)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.AiVoiceInput.isDictationSupported");
    }

    internal static ValueTask<int> BitAiVoiceInputStartDictation(this IJSRuntime jsRuntime, DotNetObjectReference<BitAiVoiceInput> dotnetObj, string? language, string ownerId)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.AiVoiceInput.startDictation", dotnetObj, language, ownerId);
    }

    internal static ValueTask BitAiVoiceInputStopDictation(this IJSRuntime jsRuntime, int handle)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiVoiceInput.stopDictation", handle);
    }
}
