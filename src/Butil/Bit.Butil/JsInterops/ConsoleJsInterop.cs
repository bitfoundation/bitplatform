using System;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class ConsoleJsInterop
{
    private static bool _isInitialized;
    private static IJSRuntime _js = default!;

    public static void Init(IJSRuntime jsRuntime)
    {
        if (_isInitialized) return;

        _isInitialized = true;
        _js = jsRuntime;
    }

    internal static void Log(params object?[]? args)
    {
        var _ = _js.InvokeVoidAsync("BitButil.console.log", args);
    }
}
