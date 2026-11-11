namespace Bit.BlazorUI;

internal static class BitAiReadAloudJsRuntimeExtensions
{
    internal static ValueTask BitAiReadAloudPrime(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiReadAloud.prime");
    }

    internal static ValueTask<int> BitAiReadAloudPlay(this IJSRuntime jsRuntime,
                                                      DotNetObjectReference<BitAiReadAloud> dotnetObj,
                                                      string? url,
                                                      DotNetStreamReference? content,
                                                      string? contentType,
                                                      double rate)
    {
        return jsRuntime.Invoke<int>("BitBlazorUI.AiReadAloud.play", dotnetObj, url, content, contentType, rate);
    }

    internal static ValueTask BitAiReadAloudStop(this IJSRuntime jsRuntime, int handle)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiReadAloud.stop", handle);
    }
}
