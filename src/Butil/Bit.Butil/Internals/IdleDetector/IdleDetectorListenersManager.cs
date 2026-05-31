using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class IdleDetectorListenersManager
{
    internal const string InvokeMethodName = "InvokeIdleDetector";

    private static readonly ConcurrentDictionary<Guid, Action<IdleState>> Listeners = [];

    internal static Guid AddListener(Action<IdleState> action)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, action);
        return id;
    }

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, IdleState state)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.Invoke(state);
    }
}
