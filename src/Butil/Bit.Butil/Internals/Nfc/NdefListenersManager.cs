using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class NdefListenersManager
{
    internal const string ReadingMethodName = "InvokeNdefReading";
    internal const string ErrorMethodName = "InvokeNdefError";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid Add(Listener listener)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, listener);
        return id;
    }

    internal static void Remove(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(ReadingMethodName)]
    public static void InvokeReading(Guid id, NdefMessage message)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnReading?.Invoke(message);
    }

    [JSInvokable(ErrorMethodName)]
    public static void InvokeError(Guid id, string message)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnError?.Invoke(message);
    }

    internal class Listener
    {
        public Action<NdefMessage>? OnReading { get; set; }
        public Action<string>? OnError { get; set; }
    }
}
