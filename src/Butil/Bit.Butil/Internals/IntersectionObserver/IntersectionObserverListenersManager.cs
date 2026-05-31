using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class IntersectionObserverListenersManager
{
    internal const string InvokeMethodName = "InvokeIntersectionObserver";

    private static readonly ConcurrentDictionary<Guid, Action<IntersectionObserverEntry[]>> Listeners = [];

    internal static Guid AddListener(Action<IntersectionObserverEntry[]> action)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, action);
        return id;
    }

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, IntersectionObserverEntry[] entries)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.Invoke(entries);
    }
}
