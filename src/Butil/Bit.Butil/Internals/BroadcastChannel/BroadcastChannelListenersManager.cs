using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class BroadcastChannelListenersManager
{
    internal const string MessageMethodName = "InvokeBroadcastChannelMessage";
    internal const string ErrorMethodName = "InvokeBroadcastChannelError";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid AddListener(Action<System.Text.Json.JsonElement>? onMessage, Action? onError)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, new Listener { OnMessage = onMessage, OnError = onError });
        return id;
    }

    internal static void RemoveListener(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(MessageMethodName)]
    public static void InvokeMessage(Guid id, System.Text.Json.JsonElement data)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.OnMessage?.Invoke(data);
    }

    [JSInvokable(ErrorMethodName)]
    public static void InvokeError(Guid id)
    {
        if (Listeners.TryGetValue(id, out var listener)) listener.OnError?.Invoke();
    }

    private class Listener
    {
        public Action<System.Text.Json.JsonElement>? OnMessage { get; set; }
        public Action? OnError { get; set; }
    }
}
