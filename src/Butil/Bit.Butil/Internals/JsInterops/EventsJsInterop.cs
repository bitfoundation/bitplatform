using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class EventsJsInterop
{
    internal static async Task AddEventListener(this IJSRuntime js,
        string elementName,
        string eventName,
        string methodName,
        Guid listenerId,
        string[] argsMembers,
        object? options = null,
        bool preventDefault = false,
        bool stopPropagation = false)
        => await js.FastInvokeVoidAsync("BitButil.events.addEventListener",
            elementName,
            eventName,
            methodName,
            listenerId,
            argsMembers,
            options,
            preventDefault,
            stopPropagation);

    internal static async Task RemoveEventListener(this IJSRuntime js,
        string elementName,
        string eventName,
        Guid[] listenerIds,
        object? options = null)
        => await js.FastInvokeVoidAsync("BitButil.events.removeEventListener",
            elementName,
            eventName,
            listenerIds,
            options);
}
