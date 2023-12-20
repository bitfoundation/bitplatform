using System;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class DomEventListenersManager
{
    internal const string InvokeMethodName = "InvokeDomEvent";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid SetListener(Action<object> action, string element, object options)
    {
        var id = Guid.NewGuid();

        Listeners.TryAdd(id, new Listener { Action = action, Element = element, Options = options });

        return id;
    }

    internal static Guid[] RemoveListener(Action<object> action, string element, object options)
    {
        var listenersToRemove = Listeners.Where(l => l.Value.Action == action && l.Value.Element == element && l.Value.Options == options).ToArray();

        return listenersToRemove.Select(l =>
        {
            Listeners.TryRemove(l.Key, out _);
            return l.Key;
        }).ToArray();
    }

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, object args)
    {
        Listeners.TryGetValue(id, out Listener? listener);
        listener?.Action.Invoke(args);
    }

    private class Listener
    {
        public string Element { get; set; } = string.Empty;
        public object Options { get; set; } = default!;
        public Action<object> Action { get; set; } = default!;
    }
}
