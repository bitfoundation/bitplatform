using System;

namespace Bit.Butil;

internal static class DomEventDispatcher
{
    private static readonly object FalseUseCapture = false;
    private static readonly object TrueUseCapture = true;

    internal static void AddEventListener<T>(string elementName, string domEvent, Action<T> listener, bool useCapture = false)
    {
        var domEventType = DomEventArgs.TypeOf(domEvent);
        var listenerType = typeof(T);

        if (listenerType != domEventType)
            throw new InvalidOperationException($"Invalid listner type ({listenerType}) for this dom event type ({domEventType})");

        if (domEventType == typeof(DomKeyboardEventArgs))
        {
            DomKeyboardEventHandler.AddListener(elementName, domEvent, listener, useCapture ? TrueUseCapture : FalseUseCapture);
        }
    }

    internal static void RemoveEventListener<T>(string elementName, string domEvent, Action<T> listener, bool useCapture = false)
    {
        var domEventType = DomEventArgs.TypeOf(domEvent);
        var listenerType = typeof(T);

        if (listenerType != domEventType)
            throw new InvalidOperationException($"Invalid listner type ({listenerType}) for this dom event type ({domEventType})");

        if (domEventType == typeof(DomKeyboardEventArgs))
        {
            DomKeyboardEventHandler.RemoveListener(elementName, domEvent, listener, useCapture ? TrueUseCapture : FalseUseCapture);
        }
    }
}
