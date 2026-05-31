using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class MutationObserverListenersManager
{
    internal const string InvokeMethodName = "InvokeMutationObserver";

    private static readonly ConcurrentDictionary<Guid, Action<MutationRecord[]>> Listeners = [];

    internal static Guid AddListener(Action<MutationRecord[]> action)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, action);
        return id;
    }

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, MutationRecord[] records)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.Invoke(records);
    }
}
