using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The <see cref="WindowManagement"/> service's screens-change subscriptions, and the callback
/// JavaScript dispatches them through.
/// </summary>
/// <remarks>
/// <see cref="DotNetObjectReference.Create{TValue}(TValue)"/> annotates its argument with
/// <c>PublicMethods</c>, so handing JavaScript the service itself preserved every public method
/// <see cref="WindowManagement"/> has - and the interop identifiers on them decide which JavaScript
/// modules a trimmed app downloads. The service's methods name three modules (<c>windowManagement</c>,
/// <c>screen</c> for <see cref="WindowManagement.IsExtended"/>, <c>permissions</c> for
/// <see cref="WindowManagement.QueryPermission"/>), so an app that subscribed to screen changes shipped
/// the other two whether it called them or not. Handing over this relay keeps the preservation to the
/// one callback JavaScript dispatches; the same reason <see cref="WindowMediaQueryInterop"/> exists.
/// <br/>
/// The <c>[JSInvokable]</c> identifier is the name <c>windowManagement.ts</c> dispatches by, so it stays
/// what it was when this moved off <see cref="WindowManagement"/>.
/// </remarks>
internal sealed class WindowManagementInterop : IDisposable
{
    internal const string InvokeMethodName = "InvokeScreensChange";

    private readonly ConcurrentDictionary<Guid, Action<ScreenDetails?>> _handlers = new();

    private DotNetObjectReference<WindowManagementInterop>? _dotNetRef;

    internal DotNetObjectReference<WindowManagementInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    internal ConcurrentDictionary<Guid, Action<ScreenDetails?>> Handlers => _handlers;

    internal bool IsEmpty => _handlers.IsEmpty;

    internal bool Remove(Guid id) => _handlers.TryRemove(id, out _);

    /// <summary>Every live id, and forgets them all - the disposal snapshot.</summary>
    internal Guid[] Drain()
    {
        var ids = _handlers.Keys.ToArray();
        _handlers.Clear();
        return ids;
    }

    /// <summary>Invoked from JS when the set of screens changes or the window moves to another one.</summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeScreensChange(Guid id, ScreenDetails? details)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(details);
    }

    public void Dispose()
    {
        _handlers.Clear();
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
