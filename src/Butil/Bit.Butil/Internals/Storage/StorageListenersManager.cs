using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class StorageListenersManager
{
    internal const string InvokeMethodName = "InvokeStorageEvent";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid AddListener(Action<StorageEvent> action, string area)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, new Listener { Action = action, Area = area });
        return id;
    }

    internal static Guid[] RemoveListener(Action<StorageEvent> action)
    {
        var toRemove = Listeners.Where(l => l.Value.Action == action).ToArray();
        return toRemove.Select(l => { Listeners.TryRemove(l.Key, out _); return l.Key; }).ToArray();
    }

    internal static void RemoveListeners(Guid[] ids)
    {
        foreach (var id in ids) Listeners.TryRemove(id, out _);
    }

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, StorageEvent evt)
    {
        if (Listeners.TryGetValue(id, out var listener) &&
            (string.IsNullOrEmpty(listener.Area) || string.Equals(listener.Area, evt.StorageArea, StringComparison.Ordinal)))
        {
            listener.Action.Invoke(evt);
        }
    }

    private class Listener
    {
        public string Area { get; set; } = string.Empty;
        public Action<StorageEvent> Action { get; set; } = default!;
    }
}
