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

    internal static ValueTask<bool> BitAiVoiceInputIsRecordingSupported(this IJSRuntime jsRuntime)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.AiVoiceRecorder.isSupported");
    }

    internal static ValueTask<int> BitAiVoiceInputStartRecording(this IJSRuntime jsRuntime, DotNetObjectReference<BitAiVoiceInput> dotnetObj, double maxDuration, double silenceTimeout)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.AiVoiceRecorder.start", dotnetObj, maxDuration, silenceTimeout);
    }

    internal static ValueTask BitAiVoiceInputStopRecording(this IJSRuntime jsRuntime, int handle, bool keep)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiVoiceRecorder.stop", handle, keep);
    }
}
