using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class ResizeObserverListenersManager
{
    internal const string InvokeMethodName = "InvokeResizeObserver";

    private static readonly ConcurrentDictionary<Guid, Action<ResizeObserverEntry[]>> Listeners = [];

    internal static Guid AddListener(Action<ResizeObserverEntry[]> action)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, action);
        return id;
    }

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, ResizeObserverEntry[] entries)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.Invoke(entries);
    }
}
