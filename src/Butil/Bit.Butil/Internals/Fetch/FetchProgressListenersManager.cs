using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class FetchProgressListenersManager
{
    internal const string InvokeMethodName = "InvokeFetchProgress";

    private static readonly ConcurrentDictionary<Guid, Action<FetchProgress>> Listeners = [];

    internal static void AddListener(Guid id, Action<FetchProgress> action) => Listeners[id] = action;

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, FetchProgress progress)
    {
        if (Listeners.TryGetValue(id, out var l)) l.Invoke(progress);
    }
}
