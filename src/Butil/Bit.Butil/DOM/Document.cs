using System;

namespace Bit.Butil;

public static class Document
{
    private const string ElementName = "document";
    private static readonly object FalseUseCapture = false;
    private static readonly object TrueUseCapture = true;

    public static void AddEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        var domEventType = DomEventArgs.TypeOf(domEvent);
        var listenerType = typeof(T);

        if (listenerType != domEventType)
            throw new InvalidOperationException($"Invalid listner type ({listenerType}) for this dom event type ({domEventType})");

        if (domEventType == typeof(DomKeyboardEventArgs))
        {
            var action = (listener as Action<DomKeyboardEventArgs>)!;
            var id = DomKeyboardEvent.SetListener(action, ElementName, useCapture ? TrueUseCapture : FalseUseCapture);
            BitButil.AddEventListener(ElementName, domEvent, DomKeyboardEvent.InvokeMethodName, id, DomKeyboardEventArgs.SelectedMembers, useCapture);
        }
    }

    public static void RemoveEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        var domEventType = DomEventArgs.TypeOf(domEvent);
        var listenerType = typeof(T);

        if (listenerType != domEventType)
            throw new InvalidOperationException($"Invalid listner type ({listenerType}) for this dom event type ({domEventType})");

        if (domEventType == typeof(DomKeyboardEventArgs))
        {
            var action = (listener as Action<DomKeyboardEventArgs>)!;
            var ids = DomKeyboardEvent.RemoveListener(action, ElementName, useCapture ? TrueUseCapture : FalseUseCapture);
            BitButil.RemoveEventListener(ElementName, domEvent, ids, useCapture);
        }
    }
}
