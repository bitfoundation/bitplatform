using System;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class EventsJsInterop
{
    private static bool _isInitialized;
    private static IJSRuntime _js = default!;

    public static void Init(IJSRuntime jsRuntime)
    {
        if (_isInitialized) return;

        _isInitialized = true;
        _js = jsRuntime;
    }

    internal static void AddEventListener(string elementName, string eventName, string dotnetMethodName, Guid dotnetListenerId, string[] selectedMembers, object? options = null)
    {
        var _ = _js.InvokeVoidAsync("BitButil.events.addEventListener", elementName, eventName, dotnetMethodName, dotnetListenerId, selectedMembers, options);
    }

    internal static void RemoveEventListener(string elementName, string eventName, Guid[] dotnetListenerIds, object? options = null)
    {
        var _ = _js.InvokeVoidAsync("BitButil.events.removeEventListener", elementName, eventName, dotnetListenerIds, options);
    }
}
