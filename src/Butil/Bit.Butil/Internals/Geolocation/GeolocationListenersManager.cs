using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.JSInterop;

namespace Bit.Butil;

public static class GeolocationListenersManager
{
    internal const string PositionMethodName = "InvokeGeolocationPosition";
    internal const string ErrorMethodName = "InvokeGeolocationError";

    private static readonly ConcurrentDictionary<Guid, Listener> Listeners = [];

    internal static Guid AddListener(Action<GeolocationPosition>? onPosition, Action<GeolocationException>? onError)
    {
        var id = Guid.NewGuid();
        Listeners.TryAdd(id, new Listener { OnPosition = onPosition, OnError = onError });
        return id;
    }

    internal static void RemoveListeners(Guid[] ids)
    {
        foreach (var id in ids) Listeners.TryRemove(id, out _);
    }

    [JSInvokable(PositionMethodName)]
    public static void InvokePosition(Guid id, GeolocationPosition position)
    {
        if (Listeners.TryGetValue(id, out var listener))
        {
            listener.OnPosition?.Invoke(position);
        }
    }

    [JSInvokable(ErrorMethodName)]
    public static void InvokeError(Guid id, int code, string message)
    {
        if (Listeners.TryGetValue(id, out var listener))
        {
            var enumCode = code switch
            {
                1 => GeolocationErrorCode.PermissionDenied,
                2 => GeolocationErrorCode.PositionUnavailable,
                3 => GeolocationErrorCode.Timeout,
                _ => GeolocationErrorCode.Unknown,
            };
            listener.OnError?.Invoke(new GeolocationException(enumCode, message));
        }
    }

    private class Listener
    {
        public Action<GeolocationPosition>? OnPosition { get; set; }
        public Action<GeolocationException>? OnError { get; set; }
    }
}
