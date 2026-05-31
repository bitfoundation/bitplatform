using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Geolocation_API">Geolocation API</see>
/// (<c>navigator.geolocation</c>).
/// </summary>
public class Geolocation(IJSRuntime js) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, byte> _watchIds = new();

    /// <summary>True when the runtime exposes <c>navigator.geolocation</c>.</summary>
    public async ValueTask<bool> IsSupported()
        => await js.Invoke<bool>("BitButil.geolocation.isSupported");

    /// <summary>
    /// Returns the device's current position once.
    /// </summary>
    /// <exception cref="GeolocationException">Thrown when permission is denied, the position
    /// can't be determined, or the call times out.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationPosition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationCoordinates))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationOptions))]
    public async Task<GeolocationPosition> GetCurrentPosition(GeolocationOptions? options = null)
    {
        var result = await js.Invoke<GeolocationCallResult>("BitButil.geolocation.getCurrentPosition", options);
        if (result.Position is not null) return result.Position;

        throw ToException(result);
    }

    /// <summary>
    /// Subscribes to continuous position updates. Use <see cref="ClearWatch(Guid)"/> with the
    /// returned id to stop. The handler runs on the Blazor sync context.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationPosition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationCoordinates))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationListenersManager))]
    public async Task<Guid> Watch(Action<GeolocationPosition>? onPosition,
                                  Action<GeolocationException>? onError = null,
                                  GeolocationOptions? options = null)
    {
        if (onPosition is null && onError is null)
            throw new ArgumentException("At least one of onPosition or onError must be provided.");

        var id = GeolocationListenersManager.AddListener(onPosition, onError);
        _watchIds.TryAdd(id, 0);

        await js.InvokeVoid("BitButil.geolocation.watchPosition",
            GeolocationListenersManager.PositionMethodName,
            GeolocationListenersManager.ErrorMethodName,
            id,
            options);

        return id;
    }

    /// <summary>Stops a previously registered watch.</summary>
    public async ValueTask ClearWatch(Guid id)
    {
        _watchIds.TryRemove(id, out _);
        GeolocationListenersManager.RemoveListeners([id]);

        if (OperatingSystem.IsBrowser() is false) return;
        await js.InvokeVoid("BitButil.geolocation.clearWatch", id);
    }

    /// <summary>
    /// Subscribe variant of <see cref="Watch"/> returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationPosition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationCoordinates))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationListenersManager))]
    public async Task<ButilSubscription> SubscribeWatch(Action<GeolocationPosition>? onPosition,
                                                        Action<GeolocationException>? onError = null,
                                                        GeolocationOptions? options = null)
    {
        var id = await Watch(onPosition, onError, options);
        return new ButilSubscription(id, () => ClearWatch(id));
    }

    /// <summary>Stops every watch this instance has started.</summary>
    public async ValueTask ClearAllWatches()
    {
        if (_watchIds.IsEmpty) return;
        var ids = _watchIds.Keys.ToArray();
        _watchIds.Clear();
        GeolocationListenersManager.RemoveListeners(ids);
        if (OperatingSystem.IsBrowser() is false) return;
        foreach (var id in ids)
        {
            await js.InvokeVoid("BitButil.geolocation.clearWatch", id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { await ClearAllWatches(); }
        catch (JSDisconnectedException) { }
        GC.SuppressFinalize(this);
    }

    private static GeolocationException ToException(GeolocationCallResult result)
    {
        var code = result.ErrorCode switch
        {
            1 => GeolocationErrorCode.PermissionDenied,
            2 => GeolocationErrorCode.PositionUnavailable,
            3 => GeolocationErrorCode.Timeout,
            _ => GeolocationErrorCode.Unknown,
        };
        return new GeolocationException(code, result.ErrorMessage ?? "Geolocation request failed.");
    }

    /// <summary>Internal — shape used to bridge a once-off call's success/error path.</summary>
    public class GeolocationCallResult
    {
        public GeolocationPosition? Position { get; set; }
        public int ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
