using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class ReportingListenersManager
{
    internal const string InvokeMethodName = "InvokeBrowserReport";

    private static readonly ConcurrentDictionary<Guid, Action<BrowserReport[]>> Listeners = [];

    internal static Guid AddListener(Action<BrowserReport[]> action)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, action);
        return id;
    }

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(InvokeMethodName)]
    public static void Invoke(Guid id, BrowserReport[] reports)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.Invoke(reports);
    }
}
