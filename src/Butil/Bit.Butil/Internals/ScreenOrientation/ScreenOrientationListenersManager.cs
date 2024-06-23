using System;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class ScreenOrientationListenersManager
{
    internal const string InvokeMethodName = "InvokeScreenOrientation";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid AddListener(Action<OrientationState> action)
    {
        var id = Guid.NewGuid();

        Listeners.TryAdd(id, new Listener { Action = action });

        return id;
    }

    internal static Guid[] RemoveListener(Action<OrientationState> action)
    {
        var listenersToRemove = Listeners.Where(l => l.Value.Action == action).ToArray();

        return listenersToRemove.Select(l =>
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
    public static void Invoke(Guid id, OrientationState state)
    {
        Listeners.TryGetValue(id, out Listener? listener);
        listener?.Action.Invoke(state);
    }

    private class Listener
    {
        public Action<OrientationState> Action { get; set; } = default!;
    }
}
