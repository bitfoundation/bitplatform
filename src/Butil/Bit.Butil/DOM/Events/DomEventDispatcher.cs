using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class DomEventDispatcher
{
    private static readonly object FalseUseCapture = false;
    private static readonly object TrueUseCapture = true;

    internal static async Task AddEventListener<T>(IJSRuntime js, string elementName, string domEvent, Action<T> listener, bool useCapture = false)
    {
        var domEventType = DomEventArgs.TypeOf(domEvent);
        var listenerType = typeof(T);

        if (listenerType != domEventType)
            throw new InvalidOperationException($"Invalid listener type ({listenerType}) for this dom event type ({domEventType})");

        if (domEventType == typeof(DomKeyboardEventArgs))
        {
            await DomKeyboardEventHandler.AddListener(js, elementName, domEvent, listener, useCapture ? TrueUseCapture : FalseUseCapture);
        }
    }

    internal static async Task RemoveEventListener<T>(IJSRuntime js, string elementName, string domEvent, Action<T> listener, bool useCapture = false)
    {
        var domEventType = DomEventArgs.TypeOf(domEvent);
        var listenerType = typeof(T);

        if (listenerType != domEventType)
            throw new InvalidOperationException($"Invalid listener type ({listenerType}) for this dom event type ({domEventType})");

        if (domEventType == typeof(DomKeyboardEventArgs))
        {
            await DomKeyboardEventHandler.RemoveListener(js, elementName, domEvent, listener, useCapture ? TrueUseCapture : FalseUseCapture);
        }
    }
}
