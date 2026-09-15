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
        object dotNetRef,
        Guid listenerId,
        string[] argsMembers,
        object? options = null,
        bool preventDefault = false,
        bool stopPropagation = false,
        double minInterval = 0)
        // minInterval travels as its own argument rather than inside `options`, which is handed to
        // addEventListener untouched: a rate limit is Butil's, not the browser's, and putting it
        // there would mean passing the browser a key it does not know.
        => await js.InvokeVoid("BitButil.events.addEventListener",
            elementName,
            eventName,
            methodName,
            dotNetRef,
            listenerId,
            argsMembers,
            options,
            preventDefault,
            stopPropagation,
            minInterval);

    internal static async Task RemoveEventListener(this IJSRuntime js,
        string elementName,
        string eventName,
        Guid[] listenerIds,
        object? options = null)
        => await js.InvokeVoid("BitButil.events.removeEventListener",
            elementName,
            eventName,
            listenerIds,
            options);
}
