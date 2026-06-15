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
    internal const string PositionMethodName = nameof(InvokePosition);
    internal const string ErrorMethodName = nameof(InvokeError);

    private readonly ConcurrentDictionary<Guid, Listener> _watches = new();

    // Per-instance callback reference: watches live on this (scoped) instance, so they're isolated
    // per circuit / WASM app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Geolocation>? _dotNetRef;
    private DotNetObjectReference<Geolocation> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.geolocation</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
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
    /// Invoked from JS for each watch position update. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(PositionMethodName)]
    public void InvokePosition(Guid id, GeolocationPosition position)
    {
        if (_watches.TryGetValue(id, out var listener)) listener.OnPosition?.Invoke(position);
    }

    /// <summary>Invoked from JS when a watch errors. See <see cref="InvokePosition"/>.</summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeError(Guid id, int code, string message)
    {
        if (_watches.TryGetValue(id, out var listener))
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

    /// <summary>
    /// Subscribes to continuous position updates. Use <see cref="ClearWatch(Guid)"/> with the
    /// returned id to stop. The handler runs on the Blazor sync context.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationPosition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationCoordinates))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationOptions))]
    public async Task<Guid> Watch(Action<GeolocationPosition>? onPosition,
                                  Action<GeolocationException>? onError = null,
                                  GeolocationOptions? options = null)
    {
        if (onPosition is null && onError is null)
            throw new ArgumentException("At least one of onPosition or onError must be provided.");

        var id = Guid.NewGuid();
        _watches.TryAdd(id, new Listener { OnPosition = onPosition, OnError = onError });

        await js.InvokeVoid("BitButil.geolocation.watchPosition", DotNetRef, id, options);

        return id;
    }

    /// <summary>Stops a previously registered watch.</summary>
    public async ValueTask ClearWatch(Guid id)
    {
        _watches.TryRemove(id, out _);

        await js.InvokeVoid("BitButil.geolocation.clearWatch", id);
    }

    /// <summary>
    /// Subscribe variant of <see cref="Watch"/> returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationPosition))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationCoordinates))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GeolocationOptions))]
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
        if (_watches.IsEmpty) return;
        var ids = _watches.Keys.ToArray();
        _watches.Clear();
        foreach (var id in ids)
        {
            await js.InvokeVoid("BitButil.geolocation.clearWatch", id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { await ClearAllWatches(); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
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

    private class Listener
    {
        public Action<GeolocationPosition>? OnPosition { get; set; }
        public Action<GeolocationException>? OnError { get; set; }
    }

    /// <summary>Internal - shape used to bridge a once-off call's success/error path.</summary>
    internal class GeolocationCallResult
    {
        public GeolocationPosition? Position { get; set; }
        public int ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
