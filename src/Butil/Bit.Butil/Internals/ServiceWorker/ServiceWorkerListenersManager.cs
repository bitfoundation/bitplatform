using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class ServiceWorkerListenersManager
{
    internal const string MessageMethodName = "InvokeServiceWorkerMessage";
    internal const string ControllerChangeMethodName = "InvokeServiceWorkerControllerChange";

    private static readonly ConcurrentDictionary<Guid, Action<JsonElement>> MessageListeners = [];
    private static readonly ConcurrentDictionary<Guid, Action> ControllerChangeListeners = [];

    internal static Guid AddMessageListener(Action<JsonElement> action)
    {
        var id = Guid.NewGuid();
        MessageListeners.TryAdd(id, action);
        return id;
    }
    internal static void RemoveMessageListener(Guid id) => MessageListeners.TryRemove(id, out _);

    internal static Guid AddControllerChangeListener(Action action)
    {
        var id = Guid.NewGuid();
        ControllerChangeListeners.TryAdd(id, action);
        return id;
    }
    internal static void RemoveControllerChangeListener(Guid id) => ControllerChangeListeners.TryRemove(id, out _);

    [JSInvokable(MessageMethodName)]
    public static void InvokeMessage(Guid id, JsonElement data)
    {
        if (MessageListeners.TryGetValue(id, out var l)) l.Invoke(data);
    }

    [JSInvokable(ControllerChangeMethodName)]
    public static void InvokeControllerChange(Guid id)
    {
        if (ControllerChangeListeners.TryGetValue(id, out var l)) l.Invoke();
    }
}
