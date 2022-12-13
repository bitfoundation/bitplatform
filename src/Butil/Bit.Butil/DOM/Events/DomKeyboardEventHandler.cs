using System;

namespace Bit.Butil;
internal static class DomKeyboardEventHandler
{

    internal static void AddListener<T>(string elementName, string domEvent, Action<T> listener, object options)
    {
        var action = (listener as Action<DomKeyboardEventArgs>)!;
        var id = DomKeyboardEvent.SetListener(action, elementName, options);
        BitButil.AddEventListener(elementName, domEvent, DomKeyboardEvent.InvokeMethodName, id, DomKeyboardEventArgs.SelectedMembers, options);
    }

    internal static void RemoveListener<T>(string elementName, string domEvent, Action<T> listener, object options)
    {
        var action = (listener as Action<DomKeyboardEventArgs>)!;
        var ids = DomKeyboardEvent.RemoveListener(action, elementName, options);
        BitButil.RemoveEventListener(elementName, domEvent, ids, options);
    }
}
