using System;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class NotificationListenersManager
{
    internal const string ClickMethodName = "InvokeNotificationClick";
    internal const string ShowMethodName = "InvokeNotificationShow";
    internal const string CloseMethodName = "InvokeNotificationClose";
    internal const string ErrorMethodName = "InvokeNotificationError";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid Add(Listener listener)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, listener);
        return id;
    }

    internal static void Remove(Guid id) => Listeners.TryRemove(id, out _);

    [JSInvokable(ClickMethodName)]
    public static void InvokeClick(Guid id)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnClick?.Invoke();
    }

    [JSInvokable(ShowMethodName)]
    public static void InvokeShow(Guid id)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnShow?.Invoke();
    }

    [JSInvokable(CloseMethodName)]
    public static void InvokeClose(Guid id)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnClose?.Invoke();
    }

    [JSInvokable(ErrorMethodName)]
    public static void InvokeError(Guid id)
    {
        if (Listeners.TryGetValue(id, out var l)) l.OnError?.Invoke();
    }

    internal class Listener
    {
        public Action? OnClick { get; set; }
        public Action? OnShow { get; set; }
        public Action? OnClose { get; set; }
        public Action? OnError { get; set; }
    }
}
