using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The <see cref="Performance"/> service's PerformanceObserver callbacks, and the handlers waiting on
/// them, kept off the service class itself.
/// </summary>
/// <remarks>
/// <see cref="DotNetObjectReference.Create{TValue}(TValue)"/> annotates its argument with
/// <c>PublicMethods</c>, so handing JavaScript the service itself would preserve every public method the
/// service has - and with them every <c>"BitButil.&lt;module&gt;.&lt;function&gt;"</c> literal in the class,
/// which is what decides the JavaScript a trimmed app downloads. An app that only reads
/// <c>Performance.Now()</c> would then ship the Web Vitals module too. Passing this small relay instead
/// keeps that preservation down to the one callback JavaScript actually dispatches.
/// <br/>
/// The <c>[JSInvokable]</c> identifier is the name <c>performance.ts</c> dispatches by, so it stays what
/// it was when this moved off <see cref="Performance"/>.
/// </remarks>
internal sealed class PerformanceObserverInterop : IDisposable
{
    internal const string InvokeMethodName = nameof(InvokePerformanceObserver);

    private readonly ConcurrentDictionary<Guid, Action<JsonElement[]>> _handlers = new();

    private DotNetObjectReference<PerformanceObserverInterop>? _dotNetRef;

    internal DotNetObjectReference<PerformanceObserverInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    internal void Add(Guid id, Action<JsonElement[]> handler) => _handlers.TryAdd(id, handler);

    internal void Remove(Guid id) => _handlers.TryRemove(id, out _);

    /// <summary>The ids of every live subscription, and forgets them - the disposal snapshot.</summary>
    internal Guid[] Drain()
    {
        var ids = new Guid[_handlers.Count];
        _handlers.Keys.CopyTo(ids, 0);
        _handlers.Clear();
        return ids;
    }

    /// <summary>Invoked from JS on each observer report.</summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokePerformanceObserver(Guid id, JsonElement[] entries)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(entries);
    }

    public void Dispose()
    {
        _handlers.Clear();
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
