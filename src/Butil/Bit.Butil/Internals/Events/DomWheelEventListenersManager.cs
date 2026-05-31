using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class DomWheelEventListenersManager
{
    internal const string InvokeMethodName = "InvokeWheelEvent";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid SetListener(Action<ButilWheelEventArgs> action, string element, object options)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, new Listener { Action = action, Element = element, Options = options });
        return id;
    }

    internal static Guid[] RemoveListener(Action<ButilWheelEventArgs> action, string element, object options)
    {
        var toRemove = Listeners
            .Where(l => l.Value.Action == action && l.Value.Element == element && l.Value.Options == options)
            .ToArray();

        return toRemove.Select(l => { Listeners.TryRemove(l.Key, out _); return l.Key; }).ToArray();
    }
    internal static void RemoveById(Guid id) => Listeners.TryRemove(id, out _);


    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, ButilWheelEventArgs args)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.Action.Invoke(args);
    }

    private class Listener
    {
        public string Element { get; set; } = string.Empty;
        public object Options { get; set; } = default!;
        public Action<ButilWheelEventArgs> Action { get; set; } = default!;
    }
}
