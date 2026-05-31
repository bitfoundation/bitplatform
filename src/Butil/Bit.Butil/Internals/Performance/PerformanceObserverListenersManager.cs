using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class PerformanceObserverListenersManager
{
    internal const string InvokeMethodName = "InvokePerformanceObserver";

    private static readonly ConcurrentDictionary<Guid, Action<JsonElement[]>> Listeners = [];

    internal static Guid AddListener(Action<JsonElement[]> action)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, action);
        return id;
    }

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, JsonElement[] entries)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.Invoke(entries);
    }
}
