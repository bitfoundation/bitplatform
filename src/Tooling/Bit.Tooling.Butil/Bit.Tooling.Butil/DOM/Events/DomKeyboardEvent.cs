using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.JSInterop;

namespace Bit.Tooling.Butil;

public class DomKeyboardEventArgs
{
    internal static readonly string[] SelectedMembers = new string[] { "code" };

    public string Code { get; set; }
}

public static class DomKeyboardEvent
{
    internal const string InvokeMethodName = "InvokeKeyboardEvent";

    private static readonly Dictionary<Guid, Listener> Listeners = new();

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
        return Listeners.Where(l => l.Value.Action == action && l.Value.Element == element && l.Value.Options == options)
                        .Select(l =>
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
        public Action<DomKeyboardEventArgs> Action { get; set; }
        public string Element { get; set; }
        public object Options { get; set; }
    }
}
