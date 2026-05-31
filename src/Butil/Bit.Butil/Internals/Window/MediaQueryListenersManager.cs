using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class MediaQueryListenersManager
{
    internal const string InvokeMethodName = "InvokeMediaQueryChange";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid AddListener(Action<MediaQueryList> action)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, new Listener { Action = action });
        return id;
    }

    internal static Guid[] RemoveListener(Action<MediaQueryList> action)
    {
        var toRemove = Listeners.Where(l => l.Value.Action == action).ToArray();

        return toRemove.Select(l =>
        {
            Listeners.TryRemove(l.Key, out _);
            return l.Key;
        }).ToArray();
    }

    internal static void RemoveListeners(Guid[] ids)
    {
        foreach (var id in ids)
        {
            Listeners.TryRemove(id, out _);
        }
    }

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, MediaQueryList state)
    {
        Listeners.TryGetValue(id, out Listener? listener);
        listener?.Action.Invoke(state);
    }

    private class Listener
    {
        public Action<MediaQueryList> Action { get; set; } = default!;
    }
}
