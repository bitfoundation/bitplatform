using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class DomKeyboardEvent
{
    internal const string InvokeMethodName = "InvokeKeyboardEvent";

    private static readonly Dictionary<Guid, Listener> Listeners = [];

    private static Action<DomKeyboardEventArgs> GetListener(Guid id)
    {
        return Listeners[id].Action;
    }

    internal static Guid SetListener(Action<DomKeyboardEventArgs> action, string element, object options)
    {
        var id = Guid.NewGuid();
        Listeners[id] = new Listener { Action = action, Element = element, Options = options };
        return id;
    }

    internal static Guid[] RemoveListener(Action<DomKeyboardEventArgs> action, string element, object options)
    {
        var listenersToRemove = Listeners.Where(l => l.Value.Action == action && l.Value.Element == element && l.Value.Options == options).ToArray();

        return listenersToRemove.Select(l =>
        {
            Listeners.Remove(l.Key);
            return l.Key;
        }).ToArray();
    }

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, DomKeyboardEventArgs args)
    {
        var listener = GetListener(id);
        listener.Invoke(args);
    }

    private class Listener
    {
        public string Element { get; set; } = string.Empty;
        public object Options { get; set; } = default!;
        public Action<DomKeyboardEventArgs> Action { get; set; } = default!;
    }
}
