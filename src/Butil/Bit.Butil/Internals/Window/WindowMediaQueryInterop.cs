using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The <see cref="Window"/> service's <c>matchMedia</c> subscriptions, and the callback JavaScript
/// dispatches them through.
/// </summary>
/// <remarks>
/// <see cref="DotNetObjectReference.Create{TValue}(TValue)"/> annotates its argument with
/// <c>PublicMethods</c>, so handing JavaScript the service itself preserved every public method
/// <see cref="Window"/> has - and the interop identifiers on them decide which JavaScript modules a
/// trimmed app downloads. Window's are spread over four modules, so a page that only read
/// <c>InnerWidth</c> was shipping the selection API, the popup registry and the media-query module with
/// it. Handing over this relay keeps that preservation to the one callback JavaScript dispatches; the
/// same reason <see cref="DomEventsInterop"/> exists for the DOM events.
/// <br/>
/// The <c>[JSInvokable]</c> identifier is the name <c>windowMediaQuery.ts</c> dispatches by, so it stays
/// what it was when this moved off <see cref="Window"/>.
/// </remarks>
internal sealed class WindowMediaQueryInterop : IDisposable
{
    internal const string MatchMediaMethodName = "InvokeMediaQueryChange";

    private readonly ConcurrentDictionary<Guid, Action<MediaQueryList>> _handlers = new();

    private DotNetObjectReference<WindowMediaQueryInterop>? _dotNetRef;

    internal DotNetObjectReference<WindowMediaQueryInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    internal bool HasSubscriptions => _handlers.IsEmpty is false;

    internal void Add(Guid id, Action<MediaQueryList> handler) => _handlers.TryAdd(id, handler);

    internal void Remove(Guid id) => _handlers.TryRemove(id, out _);

    /// <summary>The ids registered for one handler, forgetting them - <c>Unsubscribe(handler)</c>'s half.</summary>
    internal Guid[] RemoveAll(Action<MediaQueryList> handler)
    {
        var ids = _handlers.Where(entry => Equals(entry.Value, handler)).Select(entry => entry.Key).ToArray();
        foreach (var id in ids) _handlers.TryRemove(id, out _);
        return ids;
    }

    /// <summary>Every live id, and forgets them all - the disposal snapshot.</summary>
    internal Guid[] Drain()
    {
        var ids = _handlers.Keys.ToArray();
        _handlers.Clear();
        return ids;
    }

    /// <summary>Invoked from JS when a watched media query changes.</summary>
    [JSInvokable(MatchMediaMethodName)]
    public void InvokeMediaQueryChange(Guid id, MediaQueryList state)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(state);
    }

    public void Dispose()
    {
        _handlers.Clear();
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
