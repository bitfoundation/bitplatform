using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class EventsJsInterop
{
    internal static async Task AddEventListener(this IJSRuntime js,
        string elementName,
        string eventName,
        string dotnetMethodName,
        Guid dotnetListenerId,
        string[] argsMembers,
        object? options = null,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        await js.InvokeVoidAsync("BitButil.events.addEventListener",
            elementName,
            eventName,
            dotnetMethodName,
            dotnetListenerId,
            argsMembers,
            options,
            preventDefault,
            stopPropagation);
    }

    internal static async Task RemoveEventListener(this IJSRuntime js,
        string elementName,
        string eventName,
        Guid[] dotnetListenerIds,
        object? options = null)
    {
        await js.InvokeVoidAsync("BitButil.events.removeEventListener",
            elementName,
            eventName,
            dotnetListenerIds,
            options);
    }
}
