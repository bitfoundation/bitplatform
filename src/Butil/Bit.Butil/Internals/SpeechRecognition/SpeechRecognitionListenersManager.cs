using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class SpeechRecognitionListenersManager
{
    internal const string ResultMethodName = "InvokeSpeechRecognitionResult";
    internal const string ErrorMethodName = "InvokeSpeechRecognitionError";
    internal const string EndMethodName = "InvokeSpeechRecognitionEnd";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid Add(Listener listener)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, listener);
        return id;
    }

    internal static void Remove(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(ResultMethodName)]
    public static void InvokeResult(Guid id, SpeechRecognitionResult result)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnResult?.Invoke(result);
    }

    [JSInvokable(ErrorMethodName)]
    public static void InvokeError(Guid id, string message)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnError?.Invoke(message);
    }

    [JSInvokable(EndMethodName)]
    public static void InvokeEnd(Guid id)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnEnd?.Invoke();
    }

    internal class Listener
    {
        public Action<SpeechRecognitionResult>? OnResult { get; set; }
        public Action<string>? OnError { get; set; }
        public Action? OnEnd { get; set; }
    }
}
